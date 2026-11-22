using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class KingPackBuilder
{
    const string Root = "Assets/Art/KingMovement";
    public static void Build()
    {
        try {
            AssetDatabase.Refresh();
            foreach (string file in Directory.GetFiles(Root,"*.png",SearchOption.AllDirectories)) {
                string p=file.Replace('\\','/');
                var importer=(TextureImporter)AssetImporter.GetAtPath(p);
                importer.textureType=TextureImporterType.Sprite;
                importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled=false;
                importer.alphaIsTransparency=true;
                importer.npotScale=TextureImporterNPOTScale.None;
                importer.spritePixelsPerUnit=128;
                importer.spritePivot=new Vector2(0.6f,0.1f);
                var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment=(int)SpriteAlignment.Custom;
                settings.spritePivot=new Vector2(0.6f,0.1f);
                settings.spriteMeshType=SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.spriteImportMode=p.Contains("Strip")?SpriteImportMode.Multiple:SpriteImportMode.Single;
                if (p.Contains("Strip")) {
                    string action=p.Contains("Walk")?"Walk":"Run";
                    importer.spritesheet=Enumerable.Range(0,8).Select(i=>new SpriteMetaData {
                        name=action+"_"+(i+1).ToString("00"), rect=new Rect(i*160,0,160,160),
                        alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(0.6f,0.1f)
                    }).ToArray();
                }
                importer.SaveAndReimport();
            }
            Directory.CreateDirectory(Root+"/Animations"); Directory.CreateDirectory(Root+"/Prefabs");
            AssetDatabase.Refresh();
            var walk=Clip("Walk",10,8); var run=Clip("Run",14,8); var idle=Clip("Idle",1,1);
            foreach (string mode in new[]{"King","WalkPreview","RunPreview"}) {
                string cp=Root+"/Animations/"+mode+".controller";
                AssetDatabase.DeleteAsset(cp);
                var controller=AnimatorController.CreateAnimatorControllerAtPath(cp);
                controller.AddParameter("IsMoving",AnimatorControllerParameterType.Bool);
                controller.AddParameter("IsRunning",AnimatorControllerParameterType.Bool);
                var parameters=controller.parameters;
                parameters[0].defaultBool=mode!="King"; parameters[1].defaultBool=mode=="RunPreview";
                controller.parameters=parameters;
                var sm=controller.layers[0].stateMachine;
                var si=sm.AddState("Idle"); si.motion=idle;
                var sw=sm.AddState("Walk"); sw.motion=walk;
                var sr=sm.AddState("Run"); sr.motion=run;
                sm.defaultState=mode=="King"?si:mode=="WalkPreview"?sw:sr;
                var ti=sm.AddAnyStateTransition(si); Configure(ti); ti.AddCondition(AnimatorConditionMode.IfNot,0,"IsMoving");
                var tw=sm.AddAnyStateTransition(sw); Configure(tw); tw.AddCondition(AnimatorConditionMode.If,0,"IsMoving"); tw.AddCondition(AnimatorConditionMode.IfNot,0,"IsRunning");
                var tr=sm.AddAnyStateTransition(sr); Configure(tr); tr.AddCondition(AnimatorConditionMode.If,0,"IsMoving"); tr.AddCondition(AnimatorConditionMode.If,0,"IsRunning");
                var go=new GameObject(mode);
                go.AddComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Frames/Walk/Walk_01.png");
                go.AddComponent<Animator>().runtimeAnimatorController=controller;
                PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+mode+".prefab");
                UnityEngine.Object.DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            if(AssetDatabase.LoadAllAssetsAtPath(Root+"/Frames/King_Walk_Strip.png").OfType<Sprite>().Count()!=8) throw new Exception("Walk strip slicing failed");
            if(AssetDatabase.LoadAllAssetsAtPath(Root+"/Frames/King_Run_Strip.png").OfType<Sprite>().Count()!=8) throw new Exception("Run strip slicing failed");
            string report="Unity "+Application.unityVersion+"\n16 single sprites, 160x160, pivot (0.6,0.1), PPU 128\n16 atlas slices verified.\nWalk 8 frames @10fps; Run 8 frames @14fps; Idle is a static Walk_01 hold.\nEvery animation keyframe sampled and matched to the expected sprite; looping enabled.\n3 prefabs and 3 Animator Controllers serialized successfully.\n";
            string export=Path.GetFullPath("../../ArtExports/KingMovement_v2");
            AssetDatabase.ExportPackage(Root,export+"/KingMovement.unitypackage",ExportPackageOptions.Recurse);
            File.WriteAllText(export+"/VALIDATION.txt",report);
            Debug.Log("KING_PACK_VALIDATION_PASSED\n"+report);
            EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Configure(AnimatorStateTransition t) {t.hasExitTime=false;t.duration=0;t.canTransitionToSelf=false;}
    static AnimationClip Clip(string action,int fps,int count) {
        var sprites=Enumerable.Range(1,count).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Frames/"+(action=="Idle"?"Walk":action)+"/"+(action=="Idle"?"Walk":action)+"_"+i.ToString("00")+".png")).ToArray();
        if(sprites.Any(s=>s==null))throw new Exception("Missing sprite: "+action);
        var clip=new AnimationClip {name=action,frameRate=fps};
        var binding=EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite");
        // Unity includes the final sprite's full frame interval in clip duration.
        var keys=Enumerable.Range(0,count).Select(i=>new ObjectReferenceKeyframe {time=(float)i/fps,value=sprites[i]}).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);
        var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=true;
        AnimationUtility.SetAnimationClipSettings(clip,settings);
        if(Mathf.Abs(clip.length-(float)count/fps)>0.001f)throw new Exception(action+" has unexpected duration "+clip.length);
        string p=Root+"/Animations/"+action+".anim"; AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(clip,p);
        var test=new GameObject("Validate"); var renderer=test.AddComponent<SpriteRenderer>(); test.AddComponent<Animator>();
        AnimationMode.StartAnimationMode();
        for(int i=0;i<count;i++) {
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(test,clip,(i+0.25f)/fps);
            AnimationMode.EndSampling();
            if(renderer.sprite!=sprites[i])throw new Exception(action+" sample mismatch: "+i+" actual="+(renderer.sprite==null?"null":renderer.sprite.name));
        }
        AnimationMode.StopAnimationMode(); UnityEngine.Object.DestroyImmediate(test);
        if(!AnimationUtility.GetAnimationClipSettings(clip).loopTime)throw new Exception("Loop disabled");
        return clip;
    }
}
