Shader "Morioh/Hybrid Painted Hills"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Painted wooded hills with alpha", 2D) = "white" {}
        _Tint ("Layer tint", Color) = (1,1,1,1)
        _Repeat ("Panorama repeats", Range(1,4)) = 2
        _Offset ("Panorama offset", Range(0,1)) = 0
        _Cutoff ("Ridge alpha threshold", Range(.01,.5)) = .06
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Tint;
            float _Repeat, _Offset, _Cutoff;
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(float4 vertex : POSITION, float2 uv : TEXCOORD0) { v2f o; o.vertex = UnityObjectToClipPos(vertex); o.uv = uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = float2(abs(frac(i.uv.x * _Repeat + _Offset)*2-1),i.uv.y);
                float4 c = tex2D(_MainTex,uv);
                clip(c.a-_Cutoff);
                return float4(c.rgb*_Tint.rgb,1);
            }
            ENDCG
        }
    }
}
