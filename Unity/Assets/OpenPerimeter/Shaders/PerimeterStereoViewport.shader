Shader "OpenPerimeter/PerimeterStereoViewport"
{
    Properties
    {
        _MainTex ("Source Texture", 2D) = "black" {}
        _TestedEye ("Tested Eye (0=OD/Right, 1=OS/Left, 2=Binocular/Align)", Int) = 0
        _BackgroundLuminanceDAC ("Background DAC (0-255)", Float) = 31.0
        _LensDistortionK1 ("Distortion Coefficient K1", Float) = 0.34
        _LensDistortionK2 ("Distortion Coefficient K2", Float) = 0.12
        _OpticalCenterOffsetX ("Optical Center X Offset", Float) = 0.0
        _OpticalCenterOffsetY ("Optical Center Y Offset", Float) = 0.0
        _EyeReliefScale ("Eye Relief Scale", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite Off
        Cull Off

        Pass
        {
            Name "StereoSplitPass"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                int _TestedEye; // 0 = OD (right), 1 = OS (left), 2 = Binocular
                float _BackgroundLuminanceDAC;
                float _LensDistortionK1;
                float _LensDistortionK2;
                float _OpticalCenterOffsetX;
                float _OpticalCenterOffsetY;
                float _EyeReliefScale;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // Applies radial distortion model: r' = r * (1 + k1*r^2 + k2*r^4)
            float2 ApplyLensDistortion(float2 uv, float2 center)
            {
                float2 delta = uv - center;
                float r2 = dot(delta, delta);
                float distortionFactor = 1.0 + _LensDistortionK1 * r2 + _LensDistortionK2 * (r2 * r2);
                return center + delta * distortionFactor * _EyeReliefScale;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                bool isLeftHalf = (uv.x < 0.5);

                // MONOCULAR INVARIANT:
                // If TestedEye is OD (Right eye, _TestedEye == 0): Left eye (x < 0.5) MUST BE PURE BLACK (0 cd/m2).
                // If TestedEye is OS (Left eye, _TestedEye == 1): Right eye (x >= 0.5) MUST BE PURE BLACK (0 cd/m2).
                if (_TestedEye == 0 && isLeftHalf)
                {
                    return half4(0.0, 0.0, 0.0, 1.0); // Non-tested OS eye: absolute black
                }
                if (_TestedEye == 1 && !isLeftHalf)
                {
                    return half4(0.0, 0.0, 0.0, 1.0); // Non-tested OD eye: absolute black
                }

                // Determine eye-specific viewport center
                float2 eyeCenter = isLeftHalf 
                    ? float2(0.25 + _OpticalCenterOffsetX, 0.5 + _OpticalCenterOffsetY)
                    : float2(0.75 + _OpticalCenterOffsetX, 0.5 + _OpticalCenterOffsetY);

                // Map UV into eye-relative coordinates normalized to [-0.5, 0.5] range
                float2 eyeUV = uv;
                if (isLeftHalf)
                {
                    eyeUV.x = eyeUV.x * 2.0; // scale 0..0.5 to 0..1.0
                }
                else
                {
                    eyeUV.x = (eyeUV.x - 0.5) * 2.0; // scale 0.5..1.0 to 0..1.0
                }

                float2 normalizedCenter = float2(0.5 + _OpticalCenterOffsetX, 0.5 + _OpticalCenterOffsetY);
                float2 distortedUV = ApplyLensDistortion(eyeUV, normalizedCenter);

                // Check bounds: if outside lens optical boundary, render black shroud
                if (distortedUV.x < 0.0 || distortedUV.x > 1.0 || distortedUV.y < 0.0 || distortedUV.y > 1.0)
                {
                    return half4(0.0, 0.0, 0.0, 1.0);
                }

                // Sample stimulus texture
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);

                // Enforce strict grayscale luminance (R = G = B)
                half gray = col.r; // Stimulus pass is strictly monochrome
                return half4(gray, gray, gray, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
