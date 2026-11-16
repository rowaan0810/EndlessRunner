Shader "Steel Ball Run/Stand Coach Portrait"
{
    Properties
    {
        _MainTex ("Stand texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            struct Interpolators
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            Interpolators Vert(appdata_base vertex)
            {
                Interpolators output;
                output.position = UnityObjectToClipPos(vertex.vertex);
                output.uv = TRANSFORM_TEX(vertex.texcoord, _MainTex);
                output.normal = UnityObjectToWorldNormal(vertex.normal);
                return output;
            }

            fixed4 Frag(Interpolators input, fixed face : VFACE) : SV_Target
            {
                float3 normal = normalize(input.normal) * (face >= 0 ? 1 : -1);
                float key = saturate(dot(normal, normalize(float3(-0.45, 0.62, 0.62))));
                float fill = saturate(dot(normal, normalize(float3(0.70, 0.24, 0.52))));
                float edge = pow(1 - abs(normal.z), 2) * saturate(normal.y + 0.5);
                fixed3 albedo = tex2D(_MainTex, input.uv).rgb * _Color.rgb;
                return fixed4(albedo * (0.64 + 0.36 * key + 0.21 * fill) + edge * fixed3(0.12, 0.09, 0.19), 1);
            }
            ENDCG
        }
    }
}
