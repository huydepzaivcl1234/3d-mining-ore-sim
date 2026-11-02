Shader "Survivor/Projectile Glow" {
Properties {
 [HDR]_Color("Glow",Color)=(.3,.6,1,1)
 _PatternScale("Pattern Scale",Float)=1
 [HideInInspector]_SrcBlend("Source blend",Float)=5
 [HideInInspector]_DstBlend("Destination blend",Float)=1
 [HideInInspector]_ZWrite("Depth write",Float)=0
 [HideInInspector]_Cull("Cull",Float)=0
 _Mode("Core 0 Rim 1 Trail 2 Crystal 3 Cube 4",Float)=0
 _Axis("Trail axis and length",Vector)=(0,0,1,4)
}
SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass {
 Blend [_SrcBlend] [_DstBlend]
 ZWrite [_ZWrite]
 Cull [_Cull]
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct Varyings {float4 positionCS:SV_POSITION;float3 pos:TEXCOORD0;float3 normal:TEXCOORD1;float3 view:TEXCOORD2;float3 localNormal:TEXCOORD3;UNITY_VERTEX_OUTPUT_STEREO};
 CBUFFER_START(UnityPerMaterial)
 float4 _Color;float4 _Axis;float _Mode;float _PatternScale;
 CBUFFER_END
 Varyings vert(Attributes i){Varyings o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.pos=i.positionOS.xyz;o.localNormal=i.normalOS;o.normal=TransformObjectToWorldNormal(i.normalOS);o.view=GetWorldSpaceViewDir(TransformObjectToWorld(i.positionOS.xyz));return o;}
 float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 float cellEdge(float2 p){
 float2 cell=floor(p),f=frac(p);float first=10,second=10;
 [unroll]for(int y=-1;y<=1;y++){[unroll]for(int x=-1;x<=1;x++){float2 g=float2(x,y);float2 v=g+float2(hash(float3(cell+g,1)),hash(float3(cell+g,7)))-f;float d=dot(v,v);if(d<first){second=first;first=d;}else second=min(second,d);}}
 return 1-smoothstep(.018,.075,second-first);
 }
 half4 frag(Varyings i):SV_Target{
 float rim=pow(1-saturate(abs(dot(normalize(i.normal),normalize(i.view)))),2.5);
 float noise=hash(floor(i.pos*_PatternScale*16+_Time.y*2));
 float alpha=.9;float light=1;
 if(_Mode>.5&&_Mode<1.5){alpha=saturate(rim*.85);light=1.7;}
 else if(_Mode>1.5&&_Mode<2.5){float t=saturate(abs(dot(i.pos,_Axis.xyz))/max(.001,_Axis.w));alpha=pow(1-t,1.35)*(.8+.1*noise);light=1.8;}
 else if(_Mode>2.5&&_Mode<3.5){alpha=.9;light=1.1+rim*1.6+noise*.15;}
 else if(_Mode>3.5){float3 p=i.pos*_PatternScale*5;float3 n=abs(normalize(i.localNormal));float2 uv=n.z>n.y&&n.z>n.x?p.xy:n.x>n.y?p.yz:p.xz;float edgeGlow=cellEdge(uv);return half4(lerp(_Color.rgb*(.3+rim*.25),float3(3,1.6,3.5),edgeGlow),1);}
 else{light=.75+rim*.5+noise*.12;}
 return half4(_Color.rgb*light,alpha*_Color.a);
 }
 ENDHLSL
 }
}
}


