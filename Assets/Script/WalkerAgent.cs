using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class WalkerAgent : Agent
{
    public GameObject body;
    public GameObject head;
    public GameObject hips;
    // 体の中心
    public GameObject[] Center = new GameObject[3];
    // うで
    public GameObject[] Arms = new GameObject[6];
    // 足
    public GameObject[] Legs = new GameObject[6];

    // 各パーツのjoint
    public ConfigurableJoint[] Center_Joint = new ConfigurableJoint[3];
    public ConfigurableJoint[] Arml_Joint = new ConfigurableJoint[6];
    public ConfigurableJoint[] Legs_Joint = new ConfigurableJoint[6];

    private Vector3 initialBodyPosition; // 初期位置保存用
    private Quaternion initialBodyRotation; // 初期回転保存用

    private Vector3 initialHipsPosition;
    private Quaternion initialHipsRotation;

    private Vector3[] initialCenterPositions;
    private Quaternion[] initialCenterRotations;
    private Vector3[] initialArmPositions;
    private Quaternion[] initialArmRotations;
    private Vector3[] initialLegPositions;
    private Quaternion[] initialLegRotations;

    public override void Initialize()
    {
        // 初期位置と回転を保存
        initialBodyPosition = body.transform.position;
        initialBodyRotation = body.transform.rotation;

        initialHipsPosition = hips.transform.position;
        initialHipsRotation = hips.transform.rotation;

        initialCenterPositions = new Vector3[Center.Length];
        initialCenterRotations = new Quaternion[Center.Length];
        for (int i = 0; i < Center.Length; i++)
        {
            initialCenterPositions[i] = Center[i].transform.position;
            initialCenterRotations[i] = Center[i].transform.rotation;
        }

        initialArmPositions = new Vector3[Arms.Length];
        initialArmRotations = new Quaternion[Arms.Length];
        for (int i = 0; i < Arms.Length; i++)
        {
            initialArmPositions[i] = Arms[i].transform.position;
            initialArmRotations[i] = Arms[i].transform.rotation;
            // Debug.Log(ini);
        }

        initialLegPositions = new Vector3[Legs.Length];
        initialLegRotations = new Quaternion[Legs.Length];
        for (int i = 0; i < Legs.Length; i++)
        {
            initialLegPositions[i] = Legs[i].transform.position;
            initialLegRotations[i] = Legs[i].transform.rotation;
        }
    }

    public override void OnEpisodeBegin(){
        body.transform.position = new Vector3(0, initialBodyPosition.y, 0);
        ResetPartsToInitialPositions();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 体の中心の位置 (3)
        sensor.AddObservation(body.transform.position);

        // ヘッドのY位置 (1)
        sensor.AddObservation(head.transform.position.y);

        // 中心部分の回転 (3×3 = 9)
        foreach (var center in Center)
        {
            sensor.AddObservation(center.transform.rotation.eulerAngles);
        }

        // 腕の回転 (3×6 = 18)
        foreach (var arm in Arms)
        {
            sensor.AddObservation(arm.transform.rotation.eulerAngles);
        }

        // 脚の回転 (3×6 = 18)
        foreach (var leg in Legs)
        {
            sensor.AddObservation(leg.transform.rotation.eulerAngles);
        }
    }


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // デバッグ用ログ
        // Debug.Log($"Head Y ポジション: {head.transform.position.y}");
        var continuousActions = actionBuffers.ContinuousActions;

        // 中央部分の力を設定
        for (int i = 0; i < 3; i++)
        {
            float torque = continuousActions[i + 12]; // アクションバッファから値を取得
            ApplyTorqueToJoint(Center_Joint[i], torque, Vector3.up); // 上方向のトルク
        }

        // アームの動き
        for (int i = 0; i < 6; i++)
        {
            float horizontalTorque = continuousActions[i]; // 水平トルク
            ApplyTorqueToJoint(Arml_Joint[i], horizontalTorque, Vector3.right); // 横方向

            float verticalTorque = continuousActions[i + 6]; // 垂直トルク
            ApplyTorqueToJoint(Arml_Joint[i], verticalTorque, Vector3.forward); // 前後方向
        }

        // 脚の動き
        for (int i = 0; i < 6; i++)
        {
            float horizontalTorque = continuousActions[i]; // 水平トルク
            ApplyTorqueToJoint(Legs_Joint[i], horizontalTorque, Vector3.right);

            float verticalTorque = continuousActions[i + 6]; // 垂直トルク
            ApplyTorqueToJoint(Legs_Joint[i], verticalTorque, Vector3.forward);
        }

        float headposition = head.transform.position.y;
        if (headposition >= 2.1f)
        {
            AddReward(0.01f);
        }
        else if (headposition <= 2.0f)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    /// <summary>
    /// ジョイントにトルクを適用するメソッド
    /// </summary>
    /// <param name="joint">対象のジョイント</param>
    /// <param name="torque">適用するトルク量</param>
    /// <param name="axis">トルクをかける軸</param>
    private void ApplyTorqueToJoint(ConfigurableJoint joint, float torque, Vector3 axis)
    {
        if (joint == null) return;

        // トルクを計算
        Vector3 torqueVector = axis * torque * 10f; // 10fはスケーリング値で調整可能

        // 関連するRigidbodyにトルクを加える
        if (joint.connectedBody != null)
        {
            joint.connectedBody.AddTorque(torqueVector);
        }
        else
        {
            Debug.LogWarning($"Joint {joint.name} 接続されてないぞ");
        }
    }

    /// <summary>
    /// 各パーツを初期位置に戻し、力をリセットするメソッド
    /// </summary>
    private void ResetPartsToInitialPositions()
    {
        Rigidbody hipsRb = hips.GetComponent<Rigidbody>();
        if (hipsRb != null)
            hipsRb.isKinematic = true;

        // リジッドボディを取得して物理演算を一時無効化
        Rigidbody bodyRb = body.GetComponent<Rigidbody>();
        if (bodyRb != null)
            bodyRb.isKinematic = true;

        Rigidbody[] centerRbs = new Rigidbody[Center.Length];
        for (int i = 0; i < Center.Length; i++)
        {
            centerRbs[i] = Center[i].GetComponent<Rigidbody>();
            if (centerRbs[i] != null)
                centerRbs[i].isKinematic = true;
        }

        Rigidbody[] armRbs = new Rigidbody[Arms.Length];
        for (int i = 0; i < Arms.Length; i++)
        {
            armRbs[i] = Arms[i].GetComponent<Rigidbody>();
            if (armRbs[i] != null)
                armRbs[i].isKinematic = true;
        }

        Rigidbody[] legRbs = new Rigidbody[Legs.Length];
        for (int i = 0; i < Legs.Length; i++)
        {
            legRbs[i] = Legs[i].GetComponent<Rigidbody>();
            if (legRbs[i] != null)
                legRbs[i].isKinematic = true;
        }

        // 各パーツの位置と回転をリセット
        body.transform.position = initialBodyPosition;
        body.transform.rotation = initialBodyRotation;

        hips.transform.position = initialHipsPosition;
        hips.transform.rotation = initialHipsRotation;

        for (int i = 0; i < Center.Length; i++)
        {
            Center[i].transform.position = initialCenterPositions[i];
            Center[i].transform.rotation = initialCenterRotations[i];
        }

        for (int i = 0; i < Arms.Length; i++)
        {
            Arms[i].transform.position = initialArmPositions[i];
            Arms[i].transform.rotation = initialArmRotations[i];
        }

        for (int i = 0; i < Legs.Length; i++)
        {
            Legs[i].transform.position = initialLegPositions[i];
            Legs[i].transform.rotation = initialLegRotations[i];
        }

        if (hipsRb != null)
        {
            hipsRb.isKinematic = false;
            hipsRb.velocity = Vector3.zero;
            hipsRb.angularVelocity = Vector3.zero;
        }
        // 速度と角速度をリセットし、物理演算を再有効化
        if (bodyRb != null)
        {
            bodyRb.isKinematic = false;
            bodyRb.velocity = Vector3.zero;
            bodyRb.angularVelocity = Vector3.zero;
        }

        for (int i = 0; i < Center.Length; i++)
        {
            if (centerRbs[i] != null)
            {
                centerRbs[i].isKinematic = false;
                centerRbs[i].velocity = Vector3.zero;
                centerRbs[i].angularVelocity = Vector3.zero;
            }
        }

        for (int i = 0; i < Arms.Length; i++)
        {
            if (armRbs[i] != null)
            {
                armRbs[i].isKinematic = false;
                armRbs[i].velocity = Vector3.zero;
                armRbs[i].angularVelocity = Vector3.zero;
            }
        }

        for (int i = 0; i < Legs.Length; i++)
        {
            if (legRbs[i] != null)
            {
                legRbs[i].isKinematic = false;
                legRbs[i].velocity = Vector3.zero;
                legRbs[i].angularVelocity = Vector3.zero;
            }
        }
    }
}