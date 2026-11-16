// Cartoon look for the characters (URP): two-tone lighting with a soft edge, a tinted shadow colour,
// a light rim, and a dark outline. Use Tools > Towelman > Apply Toon Shading To Characters to put it on.
Shader "Towelman/ToonLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Lighting)]
        _ShadowColor("Shadow Tint", Color) = (0.74, 0.70, 0.86, 1)
        _Threshold("Light / Shadow Border", Range(0, 1)) = 0.35
        _Softness("Border Softness", Range(0.001, 0.3)) = 0.08
        _LightInfluence("Sun Color Influence", Range(0, 1)) = 0.2

        [Header(Rim)]
        _RimColor("Rim Color (alpha = strength)", Color) = (1, 1, 1, 0.15)
        _RimSize("Rim Size", Range(0, 1)) = 0.35

        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0.08, 0.04, 0.05, 1)
        _OutlineWidth("Outline Width", Range(0, 0.02)) = 0.004
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadowColor;
            half _Threshold;
            half _Softness;
            half _LightInfluence;
            half4 _RimColor;
            half _RimSize;
            half4 _OutlineColor;
            half _OutlineWidth;
        CBUFFER_END
        ENDHLSL

        // main pass: toon lighting
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vp = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vp.positionCS;
                output.positionWS = vp.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float3 normal = normalize(input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);

                // soft "half lambert" so the dark side isn't too big, then cut into light / shadow
                float ndl = dot(normal, light.direction) * 0.5 + 0.5;
                float value = ndl * light.shadowAttenuation;
                half band = smoothstep(_Threshold - _Softness, _Threshold + _Softness, value);

                // the sun's colour only tints the light a little, so skin doesn't turn yellow
                half3 sun = light.color / max(max(light.color.r, light.color.g), max(light.color.b, 0.001));
                half3 lightColor = lerp(half3(1, 1, 1), sun, _LightInfluence);
                half3 color = albedo.rgb * lerp(_ShadowColor.rgb, lightColor, band);

                // light rim on the lit side
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float rim = 1.0 - saturate(dot(viewDir, normal));
                rim = smoothstep(1.0 - _RimSize - 0.03, 1.0 - _RimSize + 0.03, rim) * band;
                color += _RimColor.rgb * _RimColor.a * rim;

                return half4(color, 1);
            }
            ENDHLSL
        }

        // outline: the model drawn again, pushed outwards, showing only its back faces
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vp = GetVertexPositionInputs(input.positionOS.xyz);

                // push outwards in screen space so the line has the same thickness at any distance or scale
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float2 dir = mul((float3x3)UNITY_MATRIX_VP, normalWS).xy;
                dir = normalize(dir + 1e-5);
                dir.x *= _ScreenParams.y / _ScreenParams.x;

                float4 clip = vp.positionCS;
                clip.xy += dir * _OutlineWidth * clip.w;
                output.positionCS = clip;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(_OutlineColor.rgb, 1);
            }
            ENDHLSL
        }

        // shadows and depth, borrowed from the standard URP Lit shader
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
