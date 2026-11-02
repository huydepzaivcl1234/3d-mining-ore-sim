#if UNITY_INCLUDE_TESTS
using System.Linq;
using GameData.Player.Script;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class Ch15PlayerAssetsTests
{
    private const string ModelFolder = "Assets/GameData/Player/Models/Ch15/";

    [TestCase("FullBody")]
    [TestCase("FPSArms")]
    [TestCase("LocalBody")]
    public void RemappedMeshesHaveValidSkinning(string part)
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ModelFolder + "Ch15_ExistingRig_" + part + ".asset");
        Assert.That(mesh, Is.Not.Null);
        Assert.That(mesh.vertexCount, Is.GreaterThan(100));
        Assert.That(mesh.bindposes.Length, Is.EqualTo(65));
        Assert.That(mesh.subMeshCount, Is.EqualTo(2));
        Assert.That(mesh.bounds.size.magnitude, Is.LessThan(5f));
        Assert.That(mesh.vertices.All(v => !float.IsNaN(v.x) && !float.IsInfinity(v.x) &&
            !float.IsNaN(v.y) && !float.IsInfinity(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.z)), Is.True);
        Assert.That(mesh.boneWeights.All(w => Mathf.Abs(w.weight0 + w.weight1 + w.weight2 + w.weight3 - 1f) < .02f &&
            w.boneIndex0 < 65 && w.boneIndex1 < 65 && w.boneIndex2 < 65 && w.boneIndex3 < 65), Is.True);
    }

    [TestCase(1001)]
    [TestCase(1002)]
    public void MaterialsKeepDiffuseAndNormalMaps(int tile)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(ModelFolder + "Ch15_" + tile + ".mat");
        Assert.That(material, Is.Not.Null);
        Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
        Assert.That(material.GetTexture("_BaseMap"), Is.Not.Null);
        var normal = material.GetTexture("_BumpMap");
        Assert.That(normal, Is.Not.Null);
        Assert.That(((TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal))).textureType,
            Is.EqualTo(TextureImporterType.NormalMap));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void GrassFootstepsContainAudio(int index)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameData/Audio/SFX/Player/Grass/footstep_grass_00" + index + ".ogg");
        Assert.That(clip, Is.Not.Null);
        Assert.That(clip.channels, Is.EqualTo(1));
        Assert.That(clip.length, Is.InRange(.02f, 2f));
        clip.LoadAudioData();
        var samples = new float[clip.samples];
        Assert.That(clip.GetData(samples, 0), Is.True);
        Assert.That(samples.Any(v => Mathf.Abs(v) > .001f), Is.True);
    }

    [Test]
    public void BothWeaponsKeepReloadTimingAndSubtleCameraKick()
    {
        foreach (string path in new[] { "Assets/GameData/Player/Weapons/PistolWeaponData.asset", "Assets/GameData/Player/Weapons/Rifle/RifleWeaponData.asset" })
        {
            var data = AssetDatabase.LoadAssetAtPath<PistolWeaponData>(path);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.cameraRecoilDegrees, Is.InRange(.05f, .5f));
            Assert.That(data.handKickback, Is.GreaterThan(0));
            Assert.That(data.reloadSeconds, Is.GreaterThan(0));
        }
    }

    [Test]
    public void PistolKeepsAuthoredReloadClip()
    {
        var data = AssetDatabase.LoadAssetAtPath<PistolWeaponData>("Assets/GameData/Player/Weapons/PistolWeaponData.asset");
        Assert.That(data.reloadProfile, Is.Not.Null);
        Assert.That(data.reloadProfile.authoredClip, Is.Not.Null);
    }
}
#endif
