Shader "Morioh/Horizon Ink"
{
    Properties { _Color ("Silhouette color", Color) = (.5,.45,.6,1) }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; };
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); return o; }
            fixed4 frag(v2f i) : SV_Target { return _Color; }
            ENDCG
        }
    }
}
