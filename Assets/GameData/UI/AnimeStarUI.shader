Shader "Survivor/UI/AnimeStar"
{
 Properties{[PerRendererData]_MainTex("Particles",2D)="black"{} _Color("Tint",Color)=(1,1,1,1)}
 SubShader{
  Tags{"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"}
  Cull Off Lighting Off ZWrite Off ZTest[unity_GUIZTestMode]
  Blend One OneMinusSrcAlpha
  Pass{
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   sampler2D _MainTex;float4 _Color;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   fixed4 frag(v2f i):SV_Target{float4 c=tex2D(_MainTex,i.uv);c.rgb=saturate(c.rgb);float a=saturate(max(c.a,max(c.r,max(c.g,c.b))));return float4(c.rgb*i.color.rgb*i.color.a,a*i.color.a);}
   ENDCG
  }
 }
}
