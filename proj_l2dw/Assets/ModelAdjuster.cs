using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ModelAdjuster : ModelAdjusterBase
{
    public override bool SupportAnimationMode => true;
    public override bool SupportExpressionMode => true;
    public override bool HasMotions => true;
    public override string Name => meta.name;
    public override string MotionTemplate => meta.formatText;
    public override string TransformTemplate => meta.transformFormatText;

    private float rootScale = 1;
    private float rootRotation = 0;

    [SerializeField]
    private Transform root;

    public override Vector3 RootPosition
    {
        get => root.localPosition;
    }

    public override Vector3 RootScale
    {
        get => pivot.localScale;
    }

    public override float RootRotation
    {
        get => rootRotation;
    }

    public override float RootScaleValue
    {
        get => rootScale;
    }

    public override Transform MainPos => webgalPoses[0].transform;
    public override int ModelCount => webgalPoses.Count;

    public override ModelDisplayMode DisplayMode => MainModel.displayMode;

    public override string curExpName => MainModel.curExpName;
    public override string curMotionName => MainModel.curMotionName;

    public override List<ExpPair> ExpPairs => MainModel.expPairs;
    public override List<MotionPair> MotionPairs => MainModel.motionPairs;

    public override MygoExp CurExp => MainModel.expMgr.curExp;
    public override MygoConfig MyGOConfig => MainModel.myGOConfig;
    [SerializeField]
    private Transform pivot;
    [SerializeField]
    private WebGalModelPos webgalPosPrefab;
    private MyGOLive2DEx MainModel => webgalPoses[0].model;
    private List<WebGalModelPos> webgalPoses = new List<WebGalModelPos>();
    
    public override Live2DParamInfoList GetEmotionEditorList()
    {
        return MainModel.emotionEditor.list;
    }

    public override void InitTransform(Vector3 pos, float scale, float rotation, bool reverseXScale)
    {
        this.reverseXScale = reverseXScale;
        SetScale(scale);
        SetRotation(rotation);
        SetPosition(pos.x, pos.y);
    }

    public override void PlayMotion(string name)
    {
        foreach (var pos in webgalPoses)
        {
            var model = pos.model;
            model.curMotionName = name;
            model.PlayMotion(name);
        }
    }

    public override void PlayExp(string name)
    {
        foreach (var pos in webgalPoses)
        {
            var model = pos.model;
            model.curExpName = name;
            model.PlayExp(name);
        }
    }

    public override bool IsMotionParamSetContains(string name)
    {
        return MainModel.emotionEditor.paramSet.Contains(name);
    }

    public override float GetMotionParamValue(string name)
    {
        return MainModel.emotionEditor.paramApplyDict[name];
    }

    public override void AddMotionParamControl(string name)
    {
        foreach (var pos in webgalPoses)
        {
            var model = pos.model;
            model.emotionEditor.AddControl(name);
        }
    }

    public override void RemoveMotionParamControl(string name)
    {
        foreach (var pos in webgalPoses)
        {
            var model = pos.model;
            model.emotionEditor.RemoveControl(name);
        }
    }

    public override void SetMotionParamValue(string name, float value)
    {
        foreach (var pos in webgalPoses)
        {
            var model = pos.model;
            model.emotionEditor.SetParam(name, value);
        }
    }

    public override void ApplyMotionParamValue()
    {
        foreach (var pos in webgalPoses)
        {
            var model = pos.model;
            model.emotionEditor.ApplyValue(model.Live2DModel);
        }
    }

    public override void CopyFromExp(MygoExp exp)
    {
        foreach (var pos in webgalPoses)
        {
            var model = pos.model;
            model.emotionEditor.CopyFromExp(exp);
            model.emotionEditor.ApplyValue(model.Live2DModel);
        }
    }

    public override string GetMotionEditorExpJson()
    {
        return MainModel.emotionEditor.ToMygoExpJson().PrintJson();
    }

    public override void Sample(string paramName, float value)
    {
        foreach (var pos in webgalPoses)
        {
            pos.model.Live2DModel.setParamFloat(paramName, value);
        }
    }

    public override void SetDisplayMode(ModelDisplayMode mode)
    {
        if (MainModel.displayMode == mode)
        {
            return;
        }

        var curMotionName = MainModel.curMotionName;
        var curExpName = MainModel.curExpName;

        foreach (var pos in webgalPoses)
        {
            var model = pos.model;
            model.displayMode = mode;
            if (mode == ModelDisplayMode.Normal)
            {
                model.emotionEditor.Reset();
                model.emotionEditor.ApplyValue(model.Live2DModel);
                if (!string.IsNullOrEmpty(curMotionName))
                {
                    model.PlayMotion(curMotionName);
                }
                if (!string.IsNullOrEmpty(curExpName))
                {
                    model.PlayExp(curExpName);
                }
            }
            else if (mode == ModelDisplayMode.EmotionEditor)
            {
                model.emotionEditor.Reset();
                model.emotionEditor.ApplyValue(model.Live2DModel);
            }
        }
    }

    public override void ReloadTextures()
    {
        foreach (var pos in webgalPoses)
        {
            var model = pos.model;
            model.ReloadTextures();
        }
    }

    public override void ReloadModels()
    {
        var expName = curExpName;
        var motionName = curMotionName;
        CreateModel();
        Adjust();
        PlayExp(expName);
        PlayMotion(motionName);
    }

    public override string GetMotionExpressionParamsText()
    {
        return MainModel.GetOutputText();
    }

    public override void CreateModel()
    {
        foreach (var pos in webgalPoses)
        {
            Destroy(pos.gameObject);
        }
        webgalPoses.Clear();

        WebGalModelPos CreateWebGalModelPos(string modelFilePath)
        {
            if (!File.Exists(modelFilePath))
            {
                Debug.LogError($"ģ���ļ�������: {modelFilePath}");
                return null;
            }

            var pos = Instantiate(webgalPosPrefab);
            var model = pos.model;
            var config = Live2dLoadUtils.LoadConfig(modelFilePath);
            model.LoadConfig(config);
            model.externalHandleRender = true;
            model.modelWidth = 1;
            model.transform.localScale = Vector3.one;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0, 0, 0);

            pos.transform.SetParent(pivot);
            pos.gameObject.SetActive(true);
            return pos;
        }

        webgalPoses.Add(CreateWebGalModelPos(meta.GetValidModelFilePath(0)));
        for (int i = 0; i < meta.modelFilePaths.Count; i++)
        {
            webgalPoses.Add(CreateWebGalModelPos(meta.GetValidModelFilePath(i + 1)));
        }

        webgalPoses.RemoveAll(pos => pos == null);
    }

    public override void Adjust()
    {
        if (!Global.__PIVOT_2_4)
        {
            pivot.transform.localPosition = Vector3.zero;
        }

        for (int i = 0; i < webgalPoses.Count; i++)
        {
            WebGalModelPos pos = webgalPoses[i];
            meta.GetModelOffset(i, out var offsetX, out var offsetY);
            pos.Adjust(offsetX, -offsetY);
        }

        if (Global.__PIVOT_2_4)
        {
            var mainPos = MainPos.localPosition;
            for (int i = 0; i < webgalPoses.Count; i++)
            {
                WebGalModelPos pos = webgalPoses[i];
                pos.transform.localPosition -= mainPos;
            }
            pivot.transform.localPosition = mainPos;
        }
        
        transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        transform.position = new Vector3(Constants.WebGalWidth * 0.01f * 0.5f * -1f, Constants.WebGalHeight * 0.01f * 0.5f * 1f, 0);
    }

    public override void SetPosition(float x, float y)
    {
        root.localPosition = new Vector3(x, y, 0);
    }

    public override void SetScale(float scale)
    {
        rootScale = scale;
        pivot.localScale = new Vector3(reverseXScale ? -rootScale : rootScale, rootScale, 1);
    }

    public override void SetReverseXScale(bool reverse)
    {
        if (reverseXScale == reverse)
            return;

        var oldPos = MainPos.position;

        reverseXScale = reverse;
        pivot.localScale = new Vector3(reverseXScale ? -rootScale : rootScale, rootScale, 1);
        
        SetCharacterWorldPosition(oldPos.x, oldPos.y);
    }

    public override void SetRotation(float rotation)
    {
        rootRotation = rotation;
        pivot.localRotation = Quaternion.Euler(0, 0, rotation);
    }

    private Vector3 GetCharacterWorldPosition(float worldX, float worldY, Transform child)
    {
        // ���� b ��ǰ��������������ڸ����� a ��ƫ����
        float offsetX = child.position.x - root.position.x;
        float offsetY = child.position.y - root.position.y;

        // �����µĸ����� a ��λ��
        float newAPositionX = worldX - offsetX;
        float newAPositionY = worldY - offsetY;

        // �����µ���������
        return new Vector3(newAPositionX, newAPositionY, root.position.z);
    }

    public override void SetCharacterWorldPosition(float worldX, float worldY)
    {
        root.position = GetCharacterWorldPosition(worldX, worldY, MainPos);
    }

    public override Vector3 GetCharacterSpecWorldPosition(int modelIndex)
    {
        var trans = webgalPoses[modelIndex].transform;
        var worldPos = trans.position;
        return root.parent.InverseTransformPoint(GetCharacterWorldPosition(worldPos.x, worldPos.y, MainPos.transform));
    }

    public override float GetWebGalRotation()
    {
        return -rootRotation * Mathf.PI / 180;
    }

    public override void CopyRotationFromRoot()
    {
        rootRotation = pivot.localRotation.eulerAngles.z;
    }

    public override void CopyScaleFromRoot()
    {
        pivot.localScale = Vector3.Scale(pivot.localScale, root.localScale);
        root.localScale = Vector3.one;
        rootScale = pivot.localScale.y;
    }

    public override void BeforeGroupTransform(Transform parent)
    {
        root.parent = parent;
    }

    public override void AfterGroupTransform(float rotationDelta)
    {
        var oldPos = MainPos.position;
        root.parent = transform;
        root.eulerAngles = Vector3.zero;
        SetRotation(rootRotation + rotationDelta);
        CopyScaleFromRoot();
        SetCharacterWorldPosition(oldPos.x, oldPos.y);
    }

    public override void DoRender()
    {
        foreach (var pos in webgalPoses)
        {
            pos.model.DoRender();
        }
    }
}
