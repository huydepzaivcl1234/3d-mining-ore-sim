var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var cards=UnityEngine.Object.FindAnyObjectByType<MiningSimulator.Ores.MiningCardSystem>();
var player=cards.Player;var combat=player.GetComponent<GameData.Player.Script.SurvivorAutoCombat>();
typeof(MiningSimulator.Ores.GameSave).GetField("testStore",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).SetValue(null,new MiningSimulator.Ores.GameSaveStore("D:/3d mining sim/Deliverables/SurvivorPolish-Figma-20261029/test-profile.json"));
player.GetComponent<MiningSimulator.Ores.CharacterHealth>().SetDamageEnabled(false);
var respawn=player.GetComponent<GameData.Player.Script.PlayerRespawn>();if(respawn)respawn.RespawnNow();
var drops=AssetDatabase.LoadAssetAtPath<MiningSimulator.Ores.MiningCardData>("Assets/GameData/Cards/CardDrops.asset");
var report=new System.Collections.Generic.Dictionary<string,object>();
string output="D:/3d mining sim/Deliverables/SurvivorPolish-Figma-20261029/";System.IO.Directory.CreateDirectory(output);
System.Collections.IEnumerator Run(){
 report["oneStatsAsset"]=player.CoreData==combat.data;
 report["survivorCombatSettings"]=new{combat.BaseDamage,combat.BaseFireRate,combat.BaseProjectileSpeed,combat.AttackRange};
 report["survivorValuesApplied"]=Mathf.Abs(combat.BaseDamage-20)<.001f&&Mathf.Abs(combat.BaseFireRate-1f/.65f)<.001f&&Mathf.Abs(combat.BaseProjectileSpeed-22)<.001f;
 int frame=Time.frameCount;
 for(int i=0;i<3;i++){
  cards.CancelChoice();Time.timeScale=1;AudioListener.pause=false;yield return new WaitForSecondsRealtime(.6f);bool opened=cards.Collect(drops.tiers[0]);yield return new WaitForSecondsRealtime(.7f);
  var buttons=(UnityEngine.UI.Button[])cards.GetType().GetField("choices",flags).GetValue(cards);
  var offer=cards.CurrentOffers[i];int previous=player.UpgradeLevel(offer.id);
  var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
  UnityEngine.EventSystems.ExecuteEvents.Execute(buttons[i].gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);yield return null;
  report["buttonTrace"+i]=new{opened,choosing=cards.IsChoosing,scale=Time.timeScale,previous,actual=player.UpgradeLevel(offer.id),active=buttons[i].gameObject.activeInHierarchy,interactable=buttons[i].IsInteractable()};report["button"+i]=opened&&!cards.IsChoosing&&Time.timeScale==1&&player.UpgradeLevel(offer.id)==previous+1;
 }
 cards.Collect(drops.tiers[0]);var list=(System.Collections.Generic.List<GameData.Player.Script.SurvivorUpgradeData.Offer>)cards.GetType().GetField("offers",flags).GetValue(cards);list.Clear();
 list.Add(new(){id=GameData.Player.Script.SurvivorUpgradeId.PhysicalDamage,rarity=0,amount=5});list.Add(new(){id=GameData.Player.Script.SurvivorUpgradeId.ProjectileSpeed,rarity=1,amount=10});list.Add(new(){id=GameData.Player.Script.SurvivorUpgradeId.MaxHealth,rarity=2,amount=15});cards.RefreshPresentation();
 yield return new WaitForSecondsRealtime(.8f);ScreenCapture.CaptureScreenshot(output+"Unity-Offers.png");yield return new WaitForSecondsRealtime(.15f);
 var panel=(RectTransform)cards.GetType().GetField("choicePanel",flags).GetValue(cards);
 report["headingFont"]=((TMPro.TMP_Text)cards.GetType().GetField("title",flags).GetValue(cards)).font.faceInfo.familyName;
 report["bodyFont"]=panel.Find("ChoiceHint").GetComponent<TMPro.TMP_Text>().font.faceInfo.familyName;
 report["offerRectangles"]=panel.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.name!="SkipOffer").Select(b=>new{name=b.name,size=((RectTransform)b.transform).sizeDelta.ToString()}).ToArray();
 var skip=panel.Find("SkipOffer").GetComponent<UnityEngine.UI.Button>();skip.onClick.Invoke();yield return null;report["skipResumes"]=!cards.IsChoosing&&Time.timeScale==1;
 var discovery=UnityEngine.Object.FindAnyObjectByType<GameData.Player.Script.SurvivorMonsterDiscovery>();if(discovery)UnityEngine.Object.Destroy(discovery.gameObject);yield return null;
 var zone=UnityEngine.Object.FindAnyObjectByType<MiningSimulator.Ores.MonsterSpawnZone>();var example=UnityEngine.Object.Instantiate(zone.MonsterEntries[0].prefab,player.transform.position+Vector3.forward*6,Quaternion.identity);example.enabled=false;example.GetComponent<MiningSimulator.Ores.CharacterHealth>().SetDamageEnabled(false);
 yield return null;
 var monsters=new[]{example};
 report["liveMonsterCount"]=monsters.Length;
 if(monsters.Length>0){
  var source=monsters[0];var entry=new MiningSimulator.Ores.MonsterSpawnEntry{prefab=source,displayName=source.name.Replace("(Clone)","")};
  GameData.Player.Script.SurvivorMonsterDiscovery.Announce(source,entry,false);GameData.Player.Script.SurvivorMonsterDiscovery.Announce(source,entry,false);
  yield return new WaitForSecondsRealtime(.15f);
  discovery=UnityEngine.Object.FindAnyObjectByType<GameData.Player.Script.SurvivorMonsterDiscovery>();
  report["deduplicated"]=discovery.AnnouncementsShown==1&&discovery.PendingCount==0;
  float angle=discovery.PreviewAngle;
  ScreenCapture.CaptureScreenshot(output+"Monster-Discovery.png");
  yield return new WaitForSecondsRealtime(.3f);report["rotates"]=Mathf.Abs(Mathf.DeltaAngle(angle,discovery.PreviewAngle))>20;
  ScreenCapture.CaptureScreenshot(output+"Monster-Discovery-Rotated.png");
  yield return new WaitForSecondsRealtime(.75f);var pop=GameObject.Find("New monster");report["hidesAfterOneSecond"]=!pop;
  report["noGameplayScriptsInPreview"]=UnityEngine.Object.FindObjectsByType<MeshRenderer>().Where(r=>r.gameObject.layer==31).All(r=>!r.GetComponent<MiningSimulator.Ores.MushroomMonster>()&&!r.GetComponent<Collider>());UnityEngine.Object.Destroy(example.gameObject);
 }else report["discoveryTestMissingMonster"]=true;
 report["noRoster"]=!GameObject.Find("Daily Monster Forecast");report["framesAdvanced"]=Time.frameCount-frame;
 System.IO.File.WriteAllText(output+"Validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
}
cards.StartCoroutine(Run());return "UI actions, Figma controls and live discovery validation running";
