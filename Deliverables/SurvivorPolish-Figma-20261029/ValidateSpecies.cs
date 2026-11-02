var cards=UnityEngine.Object.FindAnyObjectByType<MiningSimulator.Ores.MiningCardSystem>();var player=cards.Player;
typeof(MiningSimulator.Ores.GameSave).GetField("testStore",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).SetValue(null,new MiningSimulator.Ores.GameSaveStore("D:/3d mining sim/Deliverables/SurvivorPolish-Figma-20261029/species-test-profile.json"));player.GetComponent<MiningSimulator.Ores.CharacterHealth>().SetDamageEnabled(false);cards.CancelChoice();Time.timeScale=1;AudioListener.pause=false;
var old=UnityEngine.Object.FindAnyObjectByType<GameData.Player.Script.SurvivorMonsterDiscovery>();if(old)UnityEngine.Object.Destroy(old.gameObject);
string output="D:/3d mining sim/Deliverables/SurvivorPolish-Figma-20261029/";
System.Collections.IEnumerator Run(){
 yield return null;
 var zone=UnityEngine.Object.FindAnyObjectByType<MiningSimulator.Ores.MonsterSpawnZone>();bool wasPaused=MiningSimulator.Ores.MonsterSpawnZone.DebugSpawningPaused;MiningSimulator.Ores.MonsterSpawnZone.DebugSpawningPaused=true;
 var report=new System.Collections.Generic.List<object>();
 foreach(var entry in zone.MonsterEntries.OrderBy(e=>e.prefab.name=="ForestGolem"?0:1)){
  if(!entry.prefab)continue;
  var example=UnityEngine.Object.Instantiate(entry.prefab,player.transform.position+Vector3.forward*6,Quaternion.identity);example.enabled=false;
  GameData.Player.Script.SurvivorMonsterDiscovery.Announce(example,entry,false);GameData.Player.Script.SurvivorMonsterDiscovery.Announce(example,entry,false);yield return null;yield return null;UnityEngine.Object.Destroy(example.gameObject);
  yield return new WaitForSecondsRealtime(.25f);
  var discovery=UnityEngine.Object.FindAnyObjectByType<GameData.Player.Script.SurvivorMonsterDiscovery>();
  var image=discovery.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).FirstOrDefault();int colored=0;
  if(image&&image.texture is RenderTexture render){var before=RenderTexture.active;RenderTexture.active=render;var pixels=new Texture2D(render.width,render.height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,render.width,render.height),0,0);pixels.Apply();colored=pixels.GetPixels32().Count(p=>p.a>16);System.IO.File.WriteAllBytes(output+"Preview-"+entry.prefab.name+".png",pixels.EncodeToPNG());UnityEngine.Object.Destroy(pixels);RenderTexture.active=before;}
  report.Add(new{name=entry.prefab.name,visiblePixels=colored,visible=colored>100,panelActive=image&&image.gameObject.activeInHierarchy,discovery.AnnouncementsShown,discovery.PreviewAngle,duplicateNotQueued=discovery&&discovery.PendingCount==0});
  yield return new WaitForSecondsRealtime(.9f);
 }
 MiningSimulator.Ores.MonsterSpawnZone.DebugSpawningPaused=wasPaused;System.IO.File.WriteAllText(output+"SpeciesValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
}
cards.StartCoroutine(Run());return "Rendering five species through the actual discovery queue";
