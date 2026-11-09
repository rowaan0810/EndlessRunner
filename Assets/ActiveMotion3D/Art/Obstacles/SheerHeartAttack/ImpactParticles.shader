Shader "ActiveMotion3D/SheerHeartAttack/ImpactParticles"
{
    Properties
    {
        _TintColor ("Tint", Color) = (1,1,1,1)
        _Intensity ("Brightness", Range(0,4)) = 1.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _TintColor;
            float _Intensity;
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.color=v.color*_TintColor;
                o.uv=v.uv;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float glow=saturate(1-dot(p,p));
                float alpha=glow*glow*i.color.a;
                return fixed4(lerp(i.color.rgb,fixed3(1,.9,.4),glow*glow)*_Intensity,alpha);
            }
            ENDCG
        }
    }
}
