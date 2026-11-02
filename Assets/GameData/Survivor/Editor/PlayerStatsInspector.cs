using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using MiningSimulator.Ores;

[CustomEditor(typeof(PlayerStats))]
public sealed class PlayerStatsInspector : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root=new VisualElement();
        var script=new PropertyField(serializedObject.FindProperty("m_Script"));script.SetEnabled(false);root.Add(script);
        var coreField=new PropertyField(serializedObject.FindProperty("coreData"),"Stats Data");root.Add(coreField);
        var details=new VisualElement();root.Add(details);
        void Build(){
            details.Clear();var content=new VisualElement();details.Add(content);var stats=target as PlayerStats;if(!stats)return;
            if(!stats.CoreData){details.Add(new HelpBox("Gán SurvivorCombatData để chỉnh chỉ số nhân vật.",HelpBoxMessageType.Info));return;}
            var data=stats.CoreData;var source=new SerializedObject(data);
            var heading=new Label("TẤN CÔNG — CHỈ SỐ GỐC");heading.style.unityFontStyleAndWeight=FontStyle.Bold;heading.style.marginTop=8;content.Add(heading);
            content.Add(new HelpBox("Chỉ chỉnh SurvivorCombat — nguồn chỉ số duy nhất. Chỉ số thực tế bên dưới đã cộng thẻ và buff. XP không tăng máu hay sát thương.",HelpBoxMessageType.Info));
            var fields=new System.Collections.Generic.List<(FloatField field,string path)>();
            void Add(string path,string label,string hint){
                var field=new FloatField(label){isDelayed=true,tooltip=hint};
                source.Update();field.SetValueWithoutNotify(source.FindProperty(path).floatValue);
                field.RegisterValueChangedCallback(e=>{
                    if(!data)return;source.Update();float value=float.IsNaN(e.newValue)||float.IsInfinity(e.newValue)?source.FindProperty(path).floatValue:e.newValue;source.FindProperty(path).floatValue=Mathf.Max(path=="damage"?0f:.1f,value);
                    source.ApplyModifiedProperties();source.Update();field.SetValueWithoutNotify(source.FindProperty(path).floatValue);
                    SceneView.RepaintAll();
                });
                fields.Add((field,path));content.Add(field);
            }
            Add("damage","Sát thương gốc","Sát thương mỗi viên, chưa cộng level và nâng cấp.");
            Add("range","Tầm đánh (m)","Bán kính chọn mục tiêu; chọn PlayerArmature và bật Gizmos để xem.");
            Add("attackInterval","Nhịp bắn (giây/lần)","0.65 giây/lần = 1.54 lần/s. Tốc độ thực tế hiển thị bên dưới.");
            Add("projectileSpeed","Tốc độ đạn (m/s)","Tốc độ bay, khác với số lần bắn mỗi giây.");
            void RefreshFields(){if(!data)return;source.Update();foreach(var entry in fields)entry.field.SetValueWithoutNotify(source.FindProperty(entry.path).floatValue);}
            content.RegisterCallback<AttachToPanelEvent>(_=>Undo.undoRedoPerformed+=RefreshFields);
            content.RegisterCallback<DetachFromPanelEvent>(_=>Undo.undoRedoPerformed-=RefreshFields);
            if(content.panel!=null)Undo.undoRedoPerformed+=RefreshFields;
            var combat=stats.GetComponent<GameData.Player.Script.SurvivorAutoCombat>();
            if(combat){
                var combatSource=new SerializedObject(combat);
                var toggle=new Toggle("Gizmo tầm đánh"){value=combat.drawAttackRange};
                toggle.RegisterValueChangedCallback(e=>{if(!combat)return;combatSource.Update();combatSource.FindProperty("drawAttackRange").boolValue=e.newValue;combatSource.ApplyModifiedProperties();SceneView.RepaintAll();});content.Add(toggle);
            }
            var title=new Label(Application.isPlaying?"CHỈ SỐ HIỆN TẠI":"XEM TRƯỚC — CHƯA TẢI SAVE");title.style.unityFontStyleAndWeight=FontStyle.Bold;title.style.marginTop=10;content.Add(title);
            var totals=new Label();totals.style.whiteSpace=WhiteSpace.Normal;content.Add(totals);
            void RefreshTotals(){
                if(!stats)return;var health=stats.GetComponent<CharacterHealth>();
                totals.text=$"HP: {(health?health.Health:stats.MaxHealth):0.##} / {stats.MaxHealth:0.##}   |   Giáp: {(health?health.Armor:stats.Armor):0.##}   |   Kháng phép: {(health?health.MagicResistance:stats.MagicResistance):0.##}";
                if(combat)totals.text+=$"\nSát thương: {combat.EffectiveDamage:0.##} / viên\nTầm đánh: {combat.AttackRange:0.##} m\nTốc độ bắn: {combat.ShotsPerSecond:0.##} lần/s ({combat.AttackInterval:0.###} s/lần)\nTốc độ đạn: {combat.ProjectileSpeed:0.##} m/s\nSố đạn mỗi loạt: {combat.ProjectileCount}";
            }
            RefreshTotals();content.schedule.Execute(RefreshTotals).Every(250);
            content.Add(new Button(()=>{Selection.activeObject=data;EditorGUIUtility.PingObject(data);}){text="Mở toàn bộ Stats: HP, phòng thủ, di chuyển, XP…"});
        }
        coreField.RegisterValueChangeCallback(_=>details.schedule.Execute(Build));Build();return root;
    }
}
