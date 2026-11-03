Shader "KiroshiZoom/Display"
{
    Properties { _MainTex ("Zoom image", 2D) = "black" {} }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _VRChatMirrorMode;
            float _VRChatCameraMode;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                // Keep the display out of mirrors and VRChat's photo camera.
                clip(0.5 - max(_VRChatMirrorMode, _VRChatCameraMode));
                float2 p = abs(i.uv - 0.5);
                float border = step(0.485, max(p.x, p.y));
                float crosshair = (1-step(0.0015,p.x))*step(p.y,0.018)
                                + (1-step(0.0015,p.y))*step(p.x,0.018);
                fixed3 scene = tex2D(_MainTex, i.uv).rgb;
                return fixed4(lerp(scene, fixed3(0.05,1,0.8), saturate(border+crosshair)),1);
            }
            ENDCG
        }
    }
}
