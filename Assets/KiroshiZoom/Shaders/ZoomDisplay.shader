Shader "KiroshiZoom/Display"
{
    Properties
    {
        _MainTex ("Zoom image", 2D) = "black" {}
        [Toggle] _FlipY ("Flip capture vertically", Float) = 1
    }
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
            float _FlipY;
            float _VRChatMirrorMode;
            float _VRChatCameraMode;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                // Full-screen display, independent of the head mesh's screen position.
                o.pos = float4(v.uv * 2 - 1, UNITY_NEAR_CLIP_VALUE, 1);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Keep the display out of mirrors and VRChat's photo camera.
                clip(0.5 - max(_VRChatMirrorMode, _VRChatCameraMode));
                // The capture camera uses 6..30 degrees (projection Y >= 3.73).
                // Never sample its render target while that camera is rendering it.
                // A player camera with an equally narrow FOV is also suppressed.
                clip(3.7 - abs(UNITY_MATRIX_P._m11));
                // Personal mirrors do not always expose the same mirror flag.
                // Only draw for a viewpoint close to the scope's eye anchor.
                float3 eyeAnchor = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
                clip(0.35 - distance(_WorldSpaceCameraPos, eyeAnchor));
                float2 p = abs(i.uv - 0.5);
                float crosshair = (1-step(0.0015,p.x))*step(p.y,0.018)
                                + (1-step(0.0015,p.y))*step(p.x,0.018);
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 uv = (i.uv - 0.5) * float2(1, 1 / aspect) + 0.5;
                uv.y = lerp(uv.y, 1 - uv.y, _FlipY);
                fixed3 scene = tex2D(_MainTex, uv).rgb;
                return fixed4(lerp(scene, fixed3(0.05,1,0.8), saturate(crosshair)),1);
            }
            ENDCG
        }
    }
}
