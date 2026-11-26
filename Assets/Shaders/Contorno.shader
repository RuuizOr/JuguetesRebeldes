// Contorno ("inverted hull") para resaltar personajes y juguetes en URP.
Shader "JuguetesRebeldes/Contorno"
{
    Properties
    {
        _ColorContorno ("Color", Color) = (0, 0, 0, 1)
        _Grosor ("Grosor (metros)", Float) = 0.035
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Contorno"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorContorno;
                float _Grosor;
            CBUFFER_END

            struct Atributos
            {
                float4 posicion : POSITION;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 posicionHC : SV_POSITION;
            };

            Varyings vert(Atributos e)
            {
                Varyings s;
                float3 n = normalize(TransformObjectToWorldNormal(e.normal));
                float3 w = TransformObjectToWorld(e.posicion.xyz) + n * _Grosor;
                s.posicionHC = TransformWorldToHClip(w);
                return s;
            }

            half4 frag(Varyings e) : SV_Target
            {
                return (half4)_ColorContorno;
            }
            ENDHLSL
        }
    }
}
