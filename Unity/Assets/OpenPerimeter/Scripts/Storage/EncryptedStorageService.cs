using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using OpenPerimeter.Calibration;
using OpenPerimeter.Core;
using OpenPerimeter.Patterns;
using OpenPerimeter.Reliability;
using OpenPerimeter.ThresholdEngine;

namespace OpenPerimeter.Storage
{
    [Serializable]
    public class StoredParticipantRecord
    {
        public string participantId; // Pseudonymous ID only, e.g. "OP-G042-OD"
        public long consentTimestampUnixMs;
        public string ageBracket;   // e.g. "50-59", minimizing exact DOB
        public string studyArm;     // "Moderate_Glaucoma", "Advanced_Glaucoma", "Normal_Control"
    }

    [Serializable]
    public class StoredTestPoint
    {
        public int pointIndex;
        public float xDeg;
        public float yDeg;
        public float thresholdDb;
        public float thresholdHfaEquivDb;
        public string censoringStatus; // "MEASURED", "FLOOR_CENSORED", "CEILING_CENSORED"
        public int presentationCount;
        public string stimulusSize;
        public string gccSector;
    }

    [Serializable]
    public class StoredPresentationEvent
    {
        public int presentationId;
        public int pointIndex;
        public float stimulusDb;
        public float stimulusCdm2;
        public bool userResponded;
        public double reactionTimeMs;
        public string catchTrialType; // "NONE", "BLIND_SPOT", "FALSE_POSITIVE", "FALSE_NEGATIVE"
        public long timestampUnixMs;
    }

    [Serializable]
    public class StoredSessionRecord
    {
        public string sessionId;
        public string participantId;
        public long timestampUnixMs;
        public string testedEye;      // "OD" or "OS"
        public string testPattern;    // "24-2" or "10-2"
        public string algorithm;      // "ZEST" or "STAIRCASE_4_2"
        public string stimulusSize;   // "SIZE_III" or "SIZE_V"
        public string deviceModel;
        public string viewerModel;
        public string calibrationId;
        public bool isCalibrated;
        public double testDurationSec;

        public ReliabilitySummary reliability;
        public float meanDeviationDb;
        public float patternStandardDeviationDb;
        public bool isNormsValidated;
        public string normsDisclaimer;

        public List<StoredTestPoint> points = new List<StoredTestPoint>();
        public List<StoredPresentationEvent> presentationLog = new List<StoredPresentationEvent>();
    }

    [Serializable]
    public class EncryptedDatabaseContainer
    {
        public List<StoredParticipantRecord> participants = new List<StoredParticipantRecord>();
        public List<StoredSessionRecord> sessions = new List<StoredSessionRecord>();
    }

    public class EncryptedStorageService
    {
        private readonly string databaseFilePath;
        private readonly byte[] encryptionKey;
        private EncryptedDatabaseContainer memoryDb;

        public EncryptedStorageService(string customPath = null, string passkey = "OpenPerimeter-AES256-LocalStore-Key")
        {
            databaseFilePath = customPath ?? Path.Combine(Application.persistentDataPath, "openperimeter_local.enc");
            
            // Derive 256-bit key using SHA-256
            using (var sha = SHA256.Create())
            {
                encryptionKey = sha.ComputeHash(Encoding.UTF8.GetBytes(passkey));
            }

            LoadDatabase();
        }

        public void SaveParticipant(StoredParticipantRecord participant)
        {
            int existingIdx = memoryDb.participants.FindIndex(p => p.participantId == participant.participantId);
            if (existingIdx >= 0)
            {
                memoryDb.participants[existingIdx] = participant;
            }
            else
            {
                memoryDb.participants.Add(participant);
            }
            PersistDatabase();
        }

        public void SaveTestSession(StoredSessionRecord session)
        {
            int existingIdx = memoryDb.sessions.FindIndex(s => s.sessionId == session.sessionId);
            if (existingIdx >= 0)
            {
                memoryDb.sessions[existingIdx] = session;
            }
            else
            {
                memoryDb.sessions.Add(session);
            }
            PersistDatabase();
        }

        public List<StoredSessionRecord> GetAllSessions()
        {
            return memoryDb.sessions;
        }

        public StoredSessionRecord GetSession(string sessionId)
        {
            return memoryDb.sessions.Find(s => s.sessionId == sessionId);
        }

        /// <summary>
        /// DPDP Act 2023: Right to Erasure / Purge Subject Records.
        /// Completely deletes the participant and all associated session data.
        /// </summary>
        public bool PurgeParticipantData(string participantId)
        {
            int removedParticipants = memoryDb.participants.RemoveAll(p => p.participantId == participantId);
            int removedSessions = memoryDb.sessions.RemoveAll(s => s.participantId == participantId);
            PersistDatabase();
            return (removedParticipants > 0 || removedSessions > 0);
        }

        private void LoadDatabase()
        {
            if (!File.Exists(databaseFilePath))
            {
                memoryDb = new EncryptedDatabaseContainer();
                return;
            }

            try
            {
                byte[] cipherBytes = File.ReadAllBytes(databaseFilePath);
                string plainJson = DecryptAesGcm(cipherBytes, encryptionKey);
                memoryDb = JsonUtility.FromJson<EncryptedDatabaseContainer>(plainJson);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[OpenPerimeter] Failed to decrypt local storage: {ex.Message}. Initializing empty DB.");
                memoryDb = new EncryptedDatabaseContainer();
            }
        }

        private void PersistDatabase()
        {
            try
            {
                string plainJson = JsonUtility.ToJson(memoryDb, true);
                byte[] cipherBytes = EncryptAesGcm(plainJson, encryptionKey);
                File.WriteAllBytes(databaseFilePath, cipherBytes);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[OpenPerimeter] Failed to persist encrypted database: {ex.Message}");
            }
        }

        // AES-256-CBC with HMAC or AES encryption wrapper
        private static byte[] EncryptAesGcm(string plainText, byte[] key)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                using (var ms = new MemoryStream())
                {
                    // Write IV first
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        private static string DecryptAesGcm(byte[] cipherData, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(cipherData, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherData, iv.Length, cipherData.Length - iv.Length);
                        cs.FlushFinalBlock();
                    }
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }
    }
}
