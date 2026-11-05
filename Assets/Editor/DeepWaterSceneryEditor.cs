#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class DeepWaterSceneryEditor
{
    const string Folder = "Assets/BeachPrototype/DeepWater";
    static readonly System.Random Rng = new System.Random(197);
    static float Rand(float a, float b) => a + (float)Rng.NextDouble() * (b-a);
    static List<Vector3> verts;
    static List<int> triangles;
    static List<Color> colors;
    static List<Vector2> uvs;
    static void Begin() { verts=new List<Vector3>();triangles=new List<int>();colors=new List<Color>();uvs=new List<Vector2>(); }
    static void V(Vector3 p, Color c, float sway=0) { verts.Add(p);colors.Add(c);uvs.Add(new Vector2(0,sway)); }
    static void Tri(int a,int b,int c) { triangles.Add(a);triangles.Add(b);triangles.Add(c); }
    static GameObject Finish(string name, Material mat, Transform parent)
    {
        var mesh=new Mesh{name=name};mesh.SetVertices(verts);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.SetUVs(0,uvs);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Folder+"/"+name+".asset");
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return go;
    }
    static Material Material(string name, Color color, float sway=0)
    {
        var m=new Material(Shader.Find("ForestPathPrototype/Underwater Growth")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Sway",sway);m.enableInstancing=true;AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;
    }
    static void Tube(Vector3 a, Vector3 b, float radiusA, float radiusB, Color color)
    {
        var axis=(b-a).normalized;var right=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.forward:Vector3.up).normalized;var forward=Vector3.Cross(axis,right);int start=verts.Count;const int sides=6;
        for(int ring=0;ring<2;ring++)for(int i=0;i<sides;i++){float angle=i*Mathf.PI*2/sides;V((ring==0?a:b)+(right*Mathf.Cos(angle)+forward*Mathf.Sin(angle))*(ring==0?radiusA:radiusB),color);}
        for(int i=0;i<sides;i++){int n=(i+1)%sides;Tri(start+i,start+n,start+i+sides);Tri(start+n,start+n+sides,start+i+sides);}
        for(int i=1;i<sides-1;i++)Tri(start+sides,start+sides+i,start+sides+i+1);
    }
    [MenuItem("Tools/Animal Setup/Add Deep Water Scenery")]
    public static void Create()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before adding permanent underwater scenery.");
        if(GameObject.Find("Deep Water Environment")!=null)throw new Exception("Deep water scenery already exists.");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/BeachPrototype","DeepWater");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene);
        System.IO.File.Copy(scene.path,"/Users/avneesh/Documents/ChatGPT/unity/backups/deep-scenery-20261102/BeforeScenery.unity",true);
        var root=new GameObject("Deep Water Environment");Undo.RegisterCreatedObjectUndo(root,"Add deep water environment");root.transform.SetParent(GameObject.Find("Beach Prototype").transform,false);
        var terrain=Terrain.activeTerrains.First(t=>t.transform.position.z<100);
        float Ground(float x,float z) => z<0 ? -16.8f : terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
        // Blend the terrain into the distant ocean floor so the tile edge is not visible.
        var data=terrain.terrainData;AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(data),"Assets/TerrainBackups/BeforeDeepWaterScenery.asset");var h=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);
        for(int z=0;z<data.heightmapResolution;z++)for(int x=0;x<data.heightmapResolution;x++){float wz=(float)z/(data.heightmapResolution-1)*data.size.z;float wx=(float)x/(data.heightmapResolution-1)*data.size.x;float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(18,0,wz))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(100,120,wx));h[z,x]=Mathf.Lerp(h[z,x],(-16.8f-terrain.transform.position.y)/data.size.y,blend);}data.SetHeights(0,0,h);EditorUtility.SetDirty(data);
        var rockMesh=AssetDatabase.LoadAllAssetsAtPath("Assets/Holotna/Mountain/Models/Rock01.fbx").OfType<Mesh>().First(m=>m.name.Contains("LOD2"));
        var rockMat=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/ForestPathPrototype/Materials/Rock01B_ForestURP.mat")){name="Submerged Rock"};rockMat.SetColor("_BaseColor",new Color(.4f,.57f,.58f));rockMat.SetFloat("_Smoothness",.24f);AssetDatabase.CreateAsset(rockMat,Folder+"/SubmergedRock.mat");
        var combines=new List<CombineInstance>();
        void Rock(float x,float z,Vector3 size,float lift=0,bool fixedFacing=false){var b=rockMesh.bounds;var scale=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);var center=new Vector3(b.center.x,b.min.y,b.center.z);var matrix=Matrix4x4.TRS(new Vector3(x,Ground(x,z)+lift,z),fixedFacing?Quaternion.identity:Quaternion.Euler(Rand(-8,8),Rand(0,360),Rand(-8,8)),scale)*Matrix4x4.Translate(-center);combines.Add(new CombineInstance{mesh=rockMesh,transform=matrix});}
        Rock(145,-14,new Vector3(5,7,5),0,true);Rock(157,-14,new Vector3(5,7.5f,5),0,true);Rock(151,-14,new Vector3(17,3.5f,5),5.6f,true);
        foreach(var p in new[]{new Vector2(139,5),new Vector2(160,3),new Vector2(135,24),new Vector2(154,32),new Vector2(133,43),new Vector2(151,42),new Vector2(132,-16),new Vector2(171,-23),new Vector2(151,-33),new Vector2(165,-4),new Vector2(140,-22)})Rock(p.x,p.y,new Vector3(Rand(2,5),Rand(2,5),Rand(2,4)));
        for(int i=0;i<25;i++){float x=Rand(130,172),z=Rand(-30,40);float pathX=148-(z-12)*.16f;if(z>12&&Mathf.Abs(x-pathX)<5)continue;Rock(x,z,new Vector3(Rand(.5f,1.8f),Rand(.3f,1.2f),Rand(.5f,1.5f)));}
        var rocks=new Mesh{name="Deep Reef Rocks"};rocks.CombineMeshes(combines.ToArray());AssetDatabase.CreateAsset(rocks,Folder+"/DeepReefRocks.asset");var rockObject=new GameObject("Reef Rocks and Natural Arch",typeof(MeshFilter),typeof(MeshRenderer));rockObject.transform.SetParent(root.transform,false);rockObject.GetComponent<MeshFilter>().sharedMesh=rocks;rockObject.GetComponent<MeshRenderer>().sharedMaterial=rockMat;rockObject.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        var kelpMat=Material("Kelp Teal",Color.white,.28f);Begin();
        foreach(var bed in new[]{new Vector2(140,4),new Vector2(159,-2),new Vector2(135,20),new Vector2(156,27),new Vector2(132,39),new Vector2(150,44),new Vector2(136,-15),new Vector2(165,-18)})
        for(int plant=0;plant<10;plant++){float x=bed.x+Rand(-2,2),z=bed.y+Rand(-2,2),height=Rand(2.2f,5.5f),yaw=Rand(0,6.28f);var basePos=new Vector3(x,Ground(x,z)-.1f,z);var right=new Vector3(Mathf.Cos(yaw),0,Mathf.Sin(yaw));var color=Color.Lerp(new Color(.06f,.22f,.15f),new Color(.18f,.48f,.3f),Rand(0,1));int start=verts.Count;for(int row=0;row<=8;row++){float t=row/8f;float width=(Mathf.Sin(t*Mathf.PI)*.22f+.025f)*(1-t*.7f);var p=basePos+Vector3.up*(t*height)+right*(Mathf.Sin(t*8+plant)*.28f*t);V(p-right*width,Color.Lerp(color*.55f,color,t),t*t);V(p+right*width,Color.Lerp(color*.55f,color,t),t*t);if(row>0){int n=start+row*2;Tri(n-2,n-1,n);Tri(n-1,n+1,n);}}}
        Finish("Swaying Kelp Beds",kelpMat,root.transform);
        var coralMat=Material("Muted Coral",Color.white,.015f);Begin();
        foreach(var bed in new[]{new Vector2(142,0),new Vector2(158,-4),new Vector2(138,-13),new Vector2(163,-16),new Vector2(134,28),new Vector2(155,24)})
        for(int plant=0;plant<7;plant++){float x=bed.x+Rand(-1.8f,1.8f),z=bed.y+Rand(-1.8f,1.8f);var p=new Vector3(x,Ground(x,z),z);var c=Color.Lerp(new Color(.66f,.3f,.19f),new Color(.35f,.3f,.57f),Rand(0,1));var top=p+Vector3.up*Rand(.7f,1.7f);Tube(p,top,.13f,.075f,c);for(int k=0;k<3;k++){var join=Vector3.Lerp(p,top,.4f+k*.18f);var tip=join+new Vector3(Rand(-.65f,.65f),Rand(.3f,.8f),Rand(-.4f,.4f));Tube(join,tip,.07f,.025f,c);}}
        Finish("Branching Coral Clusters",coralMat,root.transform);
        var spongeMat=Material("Sea Sponges",new Color(.64f,.53f,.26f));Begin();
        for(int i=0;i<20;i++){float x=i<10?Rand(136,141):Rand(158,164),z=Rand(-14,6);var p=new Vector3(x,Ground(x,z),z);float height=Rand(.5f,1.4f),radius=Rand(.12f,.26f);int start=verts.Count;for(int ring=0;ring<3;ring++)for(int k=0;k<8;k++){float a=k*Mathf.PI/4,r=ring==2?radius*.55f:radius;V(p+new Vector3(Mathf.Cos(a)*r,ring==0?0:height,Mathf.Sin(a)*r),ring==2?Color.gray:Color.white);}for(int k=0;k<8;k++){int n=(k+1)%8;Tri(start+k,start+n,start+k+8);Tri(start+n,start+n+8,start+k+8);Tri(start+k+8,start+n+8,start+k+16);Tri(start+n+8,start+n+16,start+k+16);}}
        Finish("Tube Sponges",spongeMat,root.transform);
        var fishMat=Material("Silver Blue Fish",new Color(.38f,.65f,.76f));
        for(int school=0;school<2;school++){Begin();for(int fish=0;fish<14;fish++){var p=new Vector3(Rand(-4,4),Rand(-1.2f,1.2f),Rand(-2,2));float size=Rand(.3f,.65f);int start=verts.Count;var c=Color.Lerp(Color.white,new Color(.3f,.65f,.75f),Rand(0,1));foreach(var v in new[]{new Vector3(.6f,0,0),new Vector3(-.35f,0,0),new Vector3(0,.22f,0),new Vector3(0,-.18f,0),new Vector3(0,0,.13f),new Vector3(0,0,-.13f),new Vector3(-.72f,.24f,0),new Vector3(-.72f,-.24f,0)})V(p+v*size,c);foreach(var t in new[]{new[]{0,2,4},new[]{0,4,3},new[]{0,3,5},new[]{0,5,2},new[]{1,4,2},new[]{1,3,4},new[]{1,5,3},new[]{1,2,5},new[]{1,6,7}})Tri(start+t[0],start+t[1],start+t[2]);}var schoolObject=Finish("Fish School "+school,fishMat,root.transform);schoolObject.transform.position=new Vector3(school==0?149:161,school==0?-11:-9.5f,school==0?-5:-18);schoolObject.AddComponent<UnderwaterMotion>();}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("Deep water reef, kelp, sponges, coral and fish schools created.");
    }
}
#endif
