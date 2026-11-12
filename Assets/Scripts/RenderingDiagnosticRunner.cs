#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace KidsWorld.Performance {
public sealed class RenderingDiagnosticRunner : MonoBehaviour {
 string folder; bool running; float nextPoll;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Init() { if (Application.platform != RuntimePlatform.Android) return; var go=new GameObject("RenderingDiagnosticRunner"); DontDestroyOnLoad(go); go.AddComponent<RenderingDiagnosticRunner>(); }
 void Awake() { folder=Path.Combine(Application.persistentDataPath,"render-diagnostics"); Directory.CreateDirectory(folder); Debug.Log("[RenderDiagnostic] Ready: "+folder); }
 void Update() { if(running || Time.unscaledTime<nextPoll)return; nextPoll=Time.unscaledTime+1; var f=Path.Combine(folder,"command.txt"); if(!File.Exists(f))return; var cmd=File.ReadAllText(f).Trim(); File.Delete(f); if(cmd=="run")StartCoroutine(Run()); }
 [Serializable] class Measurement { public string test; public int frames; public float fps,meanMs,p95Ms; public string camera; public Vector3 position; public Vector3 rotation; }
 [Serializable] class AssetRow { public string name,type; public long bytes; public int width,height; public string format; }
 [Serializable] class Inventory { public AssetRow[] assets; }
 void InventoryDump() { var list=new List<AssetRow>(); foreach(var t in Resources.FindObjectsOfTypeAll<Texture>())list.Add(new AssetRow{name=t.name,type=t.GetType().Name,bytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t),width=t.width,height=t.height,format=t.graphicsFormat.ToString()}); foreach(var m in Resources.FindObjectsOfTypeAll<Mesh>())list.Add(new AssetRow{name=m.name,type="Mesh",bytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(m)}); File.WriteAllText(Path.Combine(folder,"assets.json"),JsonUtility.ToJson(new Inventory{assets=list.OrderByDescending(x=>x.bytes).ToArray()},true)); }
 IEnumerator Run() {
 running=true;
 var terrains=UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include); var densities=terrains.Select(t=>t.detailObjectDensity).ToArray();
 var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include); var shadows=lights.Select(l=>l.shadows).ToArray();
 var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include); var enabled=renderers.Select(r=>r.enabled).ToArray();
 var rp=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset; float scale=rp?rp.renderScale:1, distance=rp?rp.shadowDistance:0;
 Action restore=()=>{for(int i=0;i<terrains.Length;i++)if(terrains[i])terrains[i].detailObjectDensity=densities[i];for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].shadows=shadows[i];for(int i=0;i<renderers.Length;i++)if(renderers[i])renderers[i].enabled=enabled[i];if(rp){rp.renderScale=scale;rp.shadowDistance=distance;}};
 try {
 InventoryDump(); File.WriteAllText(Path.Combine(folder,"results.jsonl"),"");
 foreach(var test in new[]{"baseline","grass-off","shadows-off","foliage-off","water-off","scale-0.5","baseline-restored"}) {
 restore();
 if(test=="grass-off")foreach(var t in terrains)t.detailObjectDensity=0;
 if(test=="shadows-off"){foreach(var l in lights)l.shadows=LightShadows.None;if(rp)rp.shadowDistance=0;}
 if(test=="scale-0.5" && rp)rp.renderScale=0.5f;
 if(test=="foliage-off" || test=="water-off")foreach(var r in renderers){if(!r || r is SkinnedMeshRenderer)continue; bool match=r.sharedMaterials.Any(m=>m && (test=="water-off" ? m.shader.name.ToLowerInvariant().Contains("water") : new[]{"leaf","grass","bush","foliage"}.Any(k=>m.name.ToLowerInvariant().Contains(k))));if(match)r.enabled=false;}
 Debug.Log("[RenderDiagnostic] Begin "+test); File.WriteAllText(Path.Combine(folder,"status.txt"),test); yield return new WaitForSecondsRealtime(3);
 var times=new List<float>(); float end=Time.realtimeSinceStartup+10; var cam=Camera.main; var pos=cam?cam.transform.position:Vector3.zero; var rot=cam?cam.transform.eulerAngles:Vector3.zero;
 while(Time.realtimeSinceStartup<end){yield return null;times.Add(Time.unscaledDeltaTime*1000);}
 times.Sort(); var result=new Measurement{test=test,frames=times.Count,meanMs=times.Average(),fps=1000/times.Average(),p95Ms=times[Mathf.Min(times.Count-1,Mathf.FloorToInt(times.Count*.95f))],camera=cam?cam.name:"none",position=pos,rotation=rot};
 File.AppendAllText(Path.Combine(folder,"results.jsonl"),JsonUtility.ToJson(result)+"\n");Debug.Log("[RenderDiagnostic] "+JsonUtility.ToJson(result));
 }
 } finally {restore();running=false;File.WriteAllText(Path.Combine(folder,"status.txt"),"complete-restored");}
 }
}
}
#endif
