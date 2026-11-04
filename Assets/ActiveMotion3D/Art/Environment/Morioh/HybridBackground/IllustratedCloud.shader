Shader "Morioh/Hybrid Illustrated Cloud"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Twelve-cloud illustration atlas", 2D) = "white" {}
        _Tint ("Layer tint and opacity", Color) = (1,1,1,1)
        _Saturation ("Illustration saturation", Range(0,1)) = .45
        _AlphaFloor ("Remove faint edge speckles", Range(0,.2)) = .035
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Tint;
            float _Saturation, _AlphaFloor;
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(float4 vertex : POSITION, float2 uv : TEXCOORD0) { v2f o; o.vertex = UnityObjectToClipPos(vertex); o.uv = uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float4 c = tex2D(_MainTex,i.uv);
                float gray = dot(c.rgb,float3(.299,.587,.114));
                c.rgb = lerp(gray.xxx,c.rgb,_Saturation) * _Tint.rgb;
                c.a = smoothstep(_AlphaFloor,1,c.a) * _Tint.a;
                return c;
            }
            ENDCG
        }
    }
}
