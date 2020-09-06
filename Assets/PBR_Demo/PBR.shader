Shader "WX/URP/PBR"
{
    Properties
    {
    	_BaseColor("BaseColor",Color)=(1,1,1,1)
        _BaseMap("MainTex",2D)="white"{}
        [NoScaleOffset]_MaskMap("MaskMap",2D)="white"{}
        [NoScaleOffset][Normal]_NormalMap("NormalMap",2D)="Bump"{}
        _NormalScale("NormalScale",Range(0,1))=1
    }
    SubShader
    {
        Tags{
        "RenderPipeline"="UniversalRenderPipeline"
        }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "PbrFunction.hlsl"
        //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        //#pragma  shader_feature_local _ADDLIGHT_ON 
        //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
        //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
        //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
        //#pragma multi_compile _ _SHADOWS_SOFT
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        real4 _BaseColor;
        float _NormalScale;
        CBUFFER_END
        TEXTURE2D(_BaseMap);    SAMPLER(sampler_BaseMap);
        TEXTURE2D(_MaskMap);    SAMPLER(sampler_MaskMap);
        TEXTURE2D(_NormalMap);  SAMPLER(sampler_NormalMap);

         struct a2v
         {
             float4 positionOS:POSITION;
             float4 normalOS:NORMAL;
             float2 texcoord:TEXCOORD;
             float4 tangentOS:TANGENT;
             float2 lightmapUV:TEXCOORD2;//一般来说是2uv是lightmap 这里取3uv比较保险
         };
         struct v2f
         {
             float4 positionCS:SV_POSITION;
             float4 texcoord:TEXCOORD;
             float4 normalWS:NORMAL;
             float4 tangentWS:TANGENT;
             float4 BtangentWS:TEXCOORD1;
         };
        ENDHLSL

        pass
        {
        Tags{
         "LightMode"="UniversalForward"
         "RenderType"="Opaque"
            }
            HLSLPROGRAM
            #pragma vertex VERT
            #pragma fragment FRAG
            v2f VERT(a2v i)
            {
                v2f o;
                o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                o.texcoord.xy=TRANSFORM_TEX(i.texcoord,_BaseMap);
                o.texcoord.zw=i.lightmapUV;
                o.normalWS.xyz=normalize(TransformObjectToWorldNormal(i.normalOS.xyz));
                o.tangentWS.xyz=normalize(TransformObjectToWorldDir(i.tangentOS.xyz));
                o.BtangentWS.xyz=cross(o.normalWS.xyz,o.tangentWS.xyz)*i.tangentOS.w*unity_WorldTransformParams.w;
                float3 posWS=TransformObjectToWorld(i.positionOS.xyz);
                o.normalWS.w=posWS.x;
                o.tangentWS.w=posWS.y;
                o.BtangentWS.w=posWS.z;

                return o;
            }
            real4 FRAG(v2f i):SV_TARGET
            {                
                //法线部分得到世界空间法线
                float4 nortex=SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.texcoord.xy);
                float3 norTS=UnpackNormalScale(nortex,_NormalScale);
                norTS.z=sqrt(1-saturate(dot(norTS.xy,norTS.xy)));
                float3x3 T2W={i.tangentWS.xyz,i.BtangentWS.xyz,i.normalWS.xyz};
                T2W=transpose(T2W);
                float3 N=NormalizeNormalPerPixel(mul(T2W,norTS));
                //return float4(N,1);

                //计算一些可能会用到的杂七杂八的东西
                float3 positionWS=float3(i.normalWS.w,i.tangentWS.w,i.BtangentWS.w);
                real3 Albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.texcoord.xy).xyz*_BaseColor.xyz;
                float4 Mask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,i.texcoord.xy);
                float Metallic=Mask.r;
                float AO=Mask.g;
                float smoothness=Mask.a;
                float TEMProughness=1-smoothness;//中间粗糙度
                float roughness=pow(TEMProughness,2);// 粗糙度
                float3 F0=lerp(0.04,Albedo,Metallic);
                Light mainLight=GetMainLight();
                float3 L=normalize(mainLight.direction);
                float3 V=SafeNormalize(_WorldSpaceCameraPos-positionWS);
                float3 H=normalize(V+L);
                float NdotV=max(saturate(dot(N,V)),0.000001);//不取0 避免除以0的计算错误
                float NdotL=max(saturate(dot(N,L)),0.000001);
                float HdotV=max(saturate(dot(H,V)),0.000001);
                float NdotH=max(saturate(dot(H,N)),0.000001);
                float LdotH=max(saturate(dot(H,L)),0.000001);
    //直接光部分
                
                float D=D_Function(NdotH,roughness);
                //return D;
                float G=G_Function(NdotL,NdotV,roughness);
                //return G;
                float3 F=F_Function(LdotH,F0);
                //return float4(F,1);
                float3 BRDFSpeSection=D*G*F/(4*NdotL*NdotV);
                float3 DirectSpeColor=BRDFSpeSection*mainLight.color*NdotL*PI;
                //return float4(DirectSpeColor,1);
                //高光部分完成 后面是漫反射
                float3 KS=F;
                float3 KD=(1-KS)*(1-Metallic);
                float3 DirectDiffColor=KD*Albedo*mainLight.color*NdotL;//分母要除PI 但是积分后乘PI 就没写
                //return float4(DirectDiffColor,1);
                float3 DirectColor=DirectSpeColor+DirectDiffColor;
                //return float4(DirectColor,1);
    //间接光部分
                float3 SHcolor=SH_IndirectionDiff(N)*AO;
                float3 IndirKS=IndirF_Function(NdotV,F0,roughness);
                float3 IndirKD=(1-IndirKS)*(1-Metallic);
                float3 IndirDiffColor=SHcolor*IndirKD*Albedo;
                //return float4(IndirDiffColor,1);
                //漫反射部分完成 后面是高光
                float3 IndirSpeCubeColor=IndirSpeCube(N,V,roughness,AO);
                //return float4(IndirSpeCubeColor,1);
                float3 IndirSpeCubeFactor=IndirSpeFactor(roughness,smoothness,BRDFSpeSection,F0,NdotV);
                float3 IndirSpeColor=IndirSpeCubeColor*IndirSpeCubeFactor;
                //return float4(IndirSpeColor,1);
                float3 IndirColor=IndirSpeColor+IndirDiffColor;
                //return float4(IndirColor,1);
                //间接光部分计算完成
                float3 Color=IndirColor+DirectColor;
                return float4(Color,1);
            }
            ENDHLSL
        }

    }

        
}
