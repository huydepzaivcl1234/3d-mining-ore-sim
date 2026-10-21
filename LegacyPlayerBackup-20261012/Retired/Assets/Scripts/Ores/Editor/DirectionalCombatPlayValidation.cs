#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using MiningSimulator.Ores;
using StarterAssets;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Bounded, save-free fixture using the actual player's rig and authored sword clips.
[InitializeOnLoad]
public static class DirectionalCombatPlayValidation
{
    private const string Key="Mining.DirectionalCombatValidation";
    [Serializable] private class Root { public string id;public bool active; }
    [Serializable] private class Snapshot { public Root[] roots; }
    private static PlayerCombatInput combat;
    private static Animator animator;
    private static ThirdPersonController motor;
    private static StarterAssetsInputs input;
    private static CharacterController body;
    private static MushroomMonster target;
    private static GameObject obstacle;
    private static int layer,step;
    private static bool followupClicked;
    private static bool earlySpamOnly;
    private static bool ready;
    private static double started;
    private static float hpBefore;
    private static MiningOrbitCamera orbit;
    private static bool cameraTurned;
    private static Quaternion contactFacing;
    private static bool contactRecorded;
    private static int returnedFrame;
    private static int movementCase;
    private static bool launched;
    private static Vector3 initial;
    private static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    private static readonly string[] Attacks={"Sword Attack 1","Sword Attack 2","Special Attack"};

    static DirectionalCombatPlayValidation()
    { EditorApplication.playModeStateChanged+=Changed;EditorApplication.update+=Tick; }
    public static void Begin(bool cameraRegression = false, int movingInputCase = 0, bool validateEarlySpam = false)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first");
        if(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene!=null)throw new InvalidOperationException("Clear Play start override first");
        var source=UnityEngine.Object.FindFirstObjectByType<PlayerCombatInput>();
        if(source==null)throw new InvalidOperationException("No active player to validate");
        var roots=SceneManager.GetActiveScene().GetRootGameObjects();
        SessionState.SetString(Key+".roots",JsonUtility.ToJson(new Snapshot {roots=roots.Select(r=>new Root{id=GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(),active=r.activeSelf}).ToArray()}));
        SessionState.SetBool(Key+".background",Application.runInBackground);
        SessionState.SetBool(Key+".camera",cameraRegression);
        SessionState.SetInt(Key+".movement",movingInputCase);
        SessionState.SetBool(Key+".earlySpam",validateEarlySpam);
        SessionState.SetBool(Key,true);SessionState.SetString(Key+".result","Running");
        ready=false;combat=null;
        foreach(var root in roots)root.SetActive(false);
        var holder=new GameObject("Directional combat fixture");holder.SetActive(false);
        var player=UnityEngine.Object.Instantiate(source.gameObject,holder.transform);player.name="Freeflow validation player";
        foreach(var script in player.GetComponentsInChildren<MonoBehaviour>(true).Reverse())
            if(script!=null && !(script is PlayerCombatInput) && !(script is ThirdPersonController) &&
                !(script is StarterAssetsInputs) && !(script is PlayerInput) && !(script is MiningCharacterHealth))
                UnityEngine.Object.DestroyImmediate(script);
        player.GetComponent<PlayerInput>().enabled=false;
        player.GetComponent<ThirdPersonController>().enabled=false;
        Application.runInBackground=true;EditorApplication.isPlaying=true;
    }
    private static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            var fixture=Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g=>g.name=="Directional combat fixture" && g.scene.IsValid());
            if(fixture!=null)UnityEngine.Object.DestroyImmediate(fixture);
            var saved=JsonUtility.FromJson<Snapshot>(SessionState.GetString(Key+".roots",""));
            foreach(var root in saved.roots)if(GlobalObjectId.TryParse(root.id,out var id))
            {var go=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;if(go!=null)go.SetActive(root.active);}
            Application.runInBackground=SessionState.GetBool(Key+".background",false);SessionState.SetBool(Key,false);return;
        }
        if(state!=PlayModeStateChange.EnteredPlayMode)return;
        try
        {
            Application.runInBackground=true;EditorApplication.isPaused=false;
            var holder=Resources.FindObjectsOfTypeAll<GameObject>().First(g=>g.name=="Directional combat fixture"&&g.scene.IsValid());
            combat=holder.GetComponentInChildren<PlayerCombatInput>(true);
            combat.transform.position=new Vector3(14000,0,14000);initial=combat.transform.position;
            var camera=new GameObject("Freeflow validation camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.rotation=Quaternion.identity;
            orbit=null;
            if(SessionState.GetBool(Key+".camera",false))
            {
                var rig=new GameObject("Freeflow validation orbit");rig.SetActive(false);
                orbit=rig.AddComponent<MiningOrbitCamera>();
                typeof(MiningOrbitCamera).GetField("followTarget",Flags).SetValue(orbit,combat.transform);
                typeof(MiningOrbitCamera).GetField("controlledCamera",Flags).SetValue(orbit,camera);
                typeof(MiningOrbitCamera).GetField("gameData",Flags).SetValue(orbit,
                    AssetDatabase.LoadAssetAtPath<MiningGameData>("Assets/GameData/Game/MiningGameData.asset"));
                rig.SetActive(true);
            }
            combat.gameObject.SetActive(true);holder.SetActive(true);
            animator=combat.GetComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            motor=combat.GetComponent<ThirdPersonController>();motor.enabled=false;motor.Grounded=true;
            if(orbit!=null){motor.GroundLayers=1;motor.enabled=true;orbit.SetShiftLocked(true);}
            movementCase=SessionState.GetInt(Key+".movement",0);
            input=combat.GetComponent<StarterAssetsInputs>();
            input.move=movementCase==1?Vector2.left:movementCase==3||movementCase==5?Vector2.down:
                movementCase==4?Vector2.up:Vector2.right;
            input.sprint=movementCase!=0;
            if(movementCase==5)orbit.SetShiftLocked(false);
            body=combat.GetComponent<CharacterController>();
            combat.GetComponent<MiningCharacterHealth>().ConfigureSpawnHealth(10000);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=initial+Vector3.down*.5f;floor.transform.localScale=new Vector3(30,1,30);
            var victim=new GameObject("Freeflow target");victim.SetActive(false);victim.transform.position=initial+Vector3.right*3;
            target=victim.AddComponent<MushroomMonster>();victim.SetActive(true);target.Health.ConfigureSpawnHealth(10000);target.Health.ConfigureRegeneration(0,5);
            var shape=victim.GetComponent<CharacterController>();shape.height=2;shape.center=Vector3.up;shape.radius=.3f;
            Physics.SyncTransforms();
            layer=animator.GetLayerIndex("combat layer");combat.SetCombatMode(true);
            animator.ResetTrigger("DrawWeapon");animator.Play("Combat",layer,0);animator.Play("Combat",animator.GetLayerIndex("Arms Layer"),0);animator.Update(0);
            launched=movementCase==0;
            if(launched)
            {
                typeof(PlayerCombatInput).GetMethod("ProcessAttackInput",Flags).Invoke(combat,new object[]{layer,true});
                if(typeof(PlayerCombatInput).GetField("stepTarget",Flags).GetValue(combat)!=target)throw new Exception("Directional target not acquired");
            }
            earlySpamOnly=SessionState.GetBool(Key+".earlySpam",false);
            step=0;followupClicked=false;cameraTurned=false;contactRecorded=false;returnedFrame=-1;hpBefore=target.Health.Health;started=EditorApplication.timeSinceStartup;ready=true;
        }
        catch(Exception e){Finish("FAIL setup "+e.GetBaseException().Message);}
    }
    private static void Tick()
    {
        if(!ready||!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||combat==null)return;
        try
        {
            if(EditorApplication.timeSinceStartup-started>40)throw new Exception("timeout frame="+Time.frameCount+" state="+animator.GetCurrentAnimatorStateInfo(layer).shortNameHash+" phase="+animator.GetCurrentAnimatorStateInfo(layer).normalizedTime+" step="+step);
            if(!launched)
            {
                if(EditorApplication.timeSinceStartup-started<.35f)return;
                initial=combat.transform.position;
                target.transform.position=initial+Vector3.forward*3f;
                typeof(MiningOrbitCamera).GetField("yaw",Flags).SetValue(orbit,0f);
                Physics.SyncTransforms();
                typeof(PlayerCombatInput).GetMethod("ProcessAttackInput",Flags).Invoke(combat,new object[]{layer,true});
                if(typeof(PlayerCombatInput).GetField("stepTarget",Flags).GetValue(combat)!=target)
                    throw new Exception("Moving case "+movementCase+" lost the forward lunge target; camera="+Camera.main.transform.eulerAngles+
                        " state="+animator.GetCurrentAnimatorStateInfo(layer).shortNameHash+" next="+animator.GetNextAnimatorStateInfo(layer).shortNameHash+
                        " transition="+animator.IsInTransition(layer)+" armed="+animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Arms Layer")).shortNameHash+
                        " awaiting="+typeof(PlayerCombatInput).GetField("strikeAwaitingAnimator",Flags).GetValue(combat));
                launched=true;
                return;
            }
            // The fixture explicitly consumes motor intent through the same collision-safe capsule.
            if(orbit==null)body.Move(motor.CombatStepVelocity*Time.deltaTime+Vector3.down*.02f);
            var state=animator.IsInTransition(layer)?animator.GetNextAnimatorStateInfo(layer):animator.GetCurrentAnimatorStateInfo(layer);
            if(step<3 && state.shortNameHash==Animator.StringToHash(Attacks[step]))
            {
                if(orbit!=null)
                {
                    if(!combat.ControlsStrikeFacing||!motor.ExternalFacing||motor.CombatMoveMultiplier!=0f)
                        throw new Exception("Strike lost motor/facing ownership at phase "+state.normalizedTime);
                    if(movementCase==0&&!cameraTurned&&state.normalizedTime>.15f)
                    {
                        typeof(MiningOrbitCamera).GetField("yaw",Flags).SetValue(orbit,270f);
                        cameraTurned=true;
                    }
                    if(target.Health.Health<hpBefore)
                    {
                        if(!contactRecorded){contactFacing=combat.transform.rotation;contactRecorded=true;if(movementCase==0)orbit.SetShiftLocked(step%2!=0);}
                        else if(Quaternion.Angle(contactFacing,combat.transform.rotation)>1f)
                            throw new Exception("Camera redirected recovery after contact");
                    }
                    if((movementCase==0?Mathf.Abs(combat.transform.position.z-initial.z):Mathf.Abs(combat.transform.position.x-initial.x))>.2f)
                        throw new Exception("Camera-relative walking diverted the strike");
                    if(movementCase==0&&state.normalizedTime>.72f)
                        typeof(MiningOrbitCamera).GetField("yaw",Flags).SetValue(orbit,0f);
                    if(movementCase!=0 && state.normalizedTime>.15f && state.normalizedTime<.65f)
                    {
                        int foot=animator.GetLayerIndex("Combat Footwork");
                        var feet=animator.IsInTransition(foot)?animator.GetNextAnimatorStateInfo(foot):animator.GetCurrentAnimatorStateInfo(foot);
                        if(feet.shortNameHash!=state.shortNameHash)throw new Exception("Locomotion replaced strike legs: "+feet.shortNameHash);
                    }
                }
                if(earlySpamOnly && state.normalizedTime>.08f && state.normalizedTime<.6f)
                    typeof(PlayerCombatInput).GetMethod("ProcessAttackInput",Flags).Invoke(combat,new object[]{layer,true});
                if(!earlySpamOnly && step<2&&!followupClicked&&!animator.IsInTransition(layer)&&state.normalizedTime>.8f&&state.normalizedTime<.95f)
                {typeof(PlayerCombatInput).GetMethod("ProcessAttackInput",Flags).Invoke(combat,new object[]{layer,true});followupClicked=true;}
                if(step==2 && state.normalizedTime>.08f && state.normalizedTime<.9f)
                {
                    typeof(PlayerCombatInput).GetMethod("ProcessAttackInput",Flags).Invoke(combat,new object[]{layer,true});
                }
                if(state.normalizedTime>.72f && target.Health.Health>=hpBefore)throw new Exception("No animation-event contact on strike "+step+
                    " player="+combat.transform.position+" target="+target.transform.position+" forward="+combat.transform.forward+
                    " grounded="+motor.Grounded+" stepVelocity="+motor.CombatStepVelocity+" contact="+typeof(PlayerCombatInput).GetField("hitApplied",Flags).GetValue(combat));
            }
            if(earlySpamOnly)
            {
                if(state.shortNameHash==Animator.StringToHash(Attacks[1]) || state.shortNameHash==Animator.StringToHash(Attacks[2]))
                    throw new Exception("Early spam advanced to an unrequested strike");
                if(state.shortNameHash==Animator.StringToHash("Combat") && !animator.IsInTransition(layer))
                {
                    if(returnedFrame<0){returnedFrame=Time.frameCount;return;}
                    if(Time.frameCount<returnedFrame+60)return;
                    if(target.Health.Health>=hpBefore)throw new Exception("First-strike contact missing");
                    typeof(PlayerCombatInput).GetMethod("ProcessAttackInput",Flags).Invoke(combat,new object[]{layer,true});
                    if(!(bool)typeof(PlayerCombatInput).GetField("strikeAwaitingAnimator",Flags).GetValue(combat))
                        throw new Exception("Fresh click after discarded spam cannot restart");
                    Finish("PASS actual rig: early spam discarded, only first strike damages target, idle stays idle, fresh click restarts; roots/background restored");
                }
                return;
            }
            if(step<2&&followupClicked&&state.shortNameHash==Animator.StringToHash(Attacks[step+1]))
            {step++;followupClicked=false;cameraTurned=false;contactRecorded=false;hpBefore=target.Health.Health;
                if(orbit!=null){typeof(MiningOrbitCamera).GetField("yaw",Flags).SetValue(orbit,0f);if(movementCase==0)input.move=Vector2.right;}}
            if(step==2 && state.shortNameHash==Animator.StringToHash("Combat"))
            {
                if(animator.IsInTransition(layer))return; // Recovery still owns facing during the outgoing blend.
                // Editor update may observe the Animator's new state before the next
                // gameplay Update performs its ownership handoff.
                if(returnedFrame<0){returnedFrame=Time.frameCount;return;}
                if(Time.frameCount<=returnedFrame+1)return;
                // Observe an idle interval without new clicks: old spam must not open attack 1.
                if(Time.frameCount < returnedFrame+60)return;
                if(target.Health.Health>=hpBefore)throw new Exception("Third contact missing");
                if(Vector3.Distance(initial,combat.transform.position)<.5f)throw new Exception("No approach movement");
                // Move a target behind a solid wall: it must not be acquired or hit.
                target.transform.position=combat.transform.position+Vector3.right*3;
                obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=combat.transform.position+new Vector3(1.5f,1,0);obstacle.transform.localScale=new Vector3(.2f,3,3);
                Physics.SyncTransforms();
                var found=typeof(PlayerCombatInput).GetMethod("FindDirectionalTarget",Flags).Invoke(combat,new object[]{Vector3.right,true});
                if(found!=null)throw new Exception("Acquired target through wall");
                if(orbit!=null && (combat.ControlsStrikeFacing||motor.CombatMoveMultiplier!=1f))
                    throw new Exception("Locomotion was not restored after combo: owns="+combat.ControlsStrikeFacing+
                        " multiplier="+motor.CombatMoveMultiplier+" current="+animator.GetCurrentAnimatorStateInfo(layer).shortNameHash+
                        " next="+animator.GetNextAnimatorStateInfo(layer).shortNameHash+" transition="+animator.IsInTransition(layer)+
                        " was="+typeof(PlayerCombatInput).GetField("wasAttacking",Flags).GetValue(combat)+
                        " soft="+typeof(PlayerCombatInput).GetField("softAimActive",Flags).GetValue(combat)+
                        " committed="+typeof(PlayerCombatInput).GetField("strikeCommitted",Flags).GetValue(combat));
                typeof(PlayerCombatInput).GetMethod("ProcessAttackInput",Flags).Invoke(combat,new object[]{layer,true});
                if (!(bool)typeof(PlayerCombatInput).GetField("strikeAwaitingAnimator",Flags).GetValue(combat))
                    throw new Exception("Fresh click after idle could not start a new combo");
                Finish("PASS actual rig three-event sword combo, fresh link-window clicks, third-strike spam stops at idle, fresh click restarts, directional acquisition, collision-safe approach, wall rejection"+
                    (movementCase!=0?", moving input case "+movementCase+", sprint held, all three contacts and full-body strike legs":"")+
                    (orbit!=null?(movementCase==0?", real motor + 180-degree orbit during all strikes, shift-lock toggle at contact, planted recovery, locomotion handoff":", real motor, planted recovery, locomotion handoff"):"")+
                    "; original roots/background restored");
            }
        }
        catch(Exception e){Finish("FAIL "+e.GetBaseException().Message);}
    }
    private static void Finish(string result)
    {ready=false;SessionState.SetString(Key+".result",result);EditorApplication.isPlaying=false;}
}
#endif
