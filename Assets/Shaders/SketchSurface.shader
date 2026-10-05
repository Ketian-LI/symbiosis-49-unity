Shader "Symbiosis/Sketch Surface"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _InkColor ("Ink Color", Color) = (0.10, 0.14, 0.19, 1)
        _InkStrength ("Ink Strength", Range(0, 1)) = 0
        _InkWidthPixels ("Ink Width In Pixels", Range(0.5, 3)) = 1.25
        _PaperGrain ("Paper Grain", Range(0, 0.12)) = 0.025
        _GrainFrequency ("Grain Frequency", Range(4, 80)) = 38
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "SketchForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _InkColor;
                half _InkStrength;
                half _InkWidthPixels;
                half _PaperGrain;
                half _GrainFrequency;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.shadowCoord = GetShadowCoord(position);
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                Light mainLight = GetMainLight(input.shadowCoord);
                float lightFacing = saturate(dot(normalize(input.normalWS), mainLight.direction));
                float paperLight = (0.84 + 0.16 * lightFacing) *
                                   lerp(0.78, 1.0, mainLight.shadowAttenuation);

                // Fine, stable world-space variation softens otherwise flat low-poly faces.
                float grain = Hash21(floor(input.positionWS.xz * _GrainFrequency));
                float grainVisibility = saturate(1.0 -
                    max(fwidth(input.positionWS.x), fwidth(input.positionWS.z)) *
                    _GrainFrequency * 0.5);
                float grainTone = 1.0 + (grain - 0.5) * _PaperGrain * grainVisibility;
                half3 surface = _BaseColor.rgb * paperLight * grainTone;

                // Unity's box primitive gives each face a 0..1 UV island. Measuring
                // the border in screen pixels keeps the ink fine at different zooms.
                float2 uvPixel = max(fwidth(input.uv), float2(0.00001, 0.00001));
                float2 edgePixels = min(input.uv, 1.0 - input.uv) / uvPixel;
                float nearestEdge = min(edgePixels.x, edgePixels.y);
                float facePixels = min(1.0 / uvPixel.x, 1.0 / uvPixel.y);
                float smallFaceFade = smoothstep(7.0, 18.0, facePixels);
                float ink = (1.0 - smoothstep(_InkWidthPixels - 0.5,
                                               _InkWidthPixels + 0.5,
                                               nearestEdge)) *
                            _InkStrength * smallFaceFade;
                surface = lerp(surface, _InkColor.rgb, ink * 0.88);
                return half4(surface, _BaseColor.a);
            }
            ENDHLSL
        }

        // Keep the existing room and furniture shadow silhouettes.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    Fallback "Universal Render Pipeline/Lit"
}
