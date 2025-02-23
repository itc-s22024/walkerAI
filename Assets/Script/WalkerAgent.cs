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

    private float currentcenterPosition;
    private float previouscenterPosition;

    private float[] currentlegPosition = new float[6];
    private float[] previouslegPosition = new float[6];

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
        }

        initialLegPositions = new Vector3[Legs.Length];
        initialLegRotations = new Quaternion[Legs.Length];
        for (int i = 0; i < Legs.Length; i++)
        {
            initialLegPositions[i] = Legs[i].transform.position;
            initialLegRotations[i] = Legs[i].transform.rotation;

            previouslegPosition[i] = Legs[i].transform.position.x;
        }
        
        for (int i = 0; i < Center.Length; i++)
        {
            previouscenterPosition += Center[i].transform.position[0];
        }
        // for (int i = 0; i < Legs.Length; i++)
        // {
        //     previousPosition += Legs[i].transform.position[0];
        // }
        previouscenterPosition += hips.transform.position.x;
        previouscenterPosition = previouscenterPosition / 4;
    }

    public override void OnEpisodeBegin(){
        body.transform.position = new Vector3(0, initialBodyPosition.y, 0);
        ResetPartsToInitialPositions();
    }

   public override void CollectObservations(VectorSensor sensor)
    {
        // 位置情報の正規化 (位置範囲 [0, 10])
        Vector3 normalizedPosition = body.transform.position / 10.0f;
        sensor.AddObservation(normalizedPosition);

        // 頭の高さの正規化 (最大高さ 2.0f)
        float normalizedHeight = head.transform.position.y / 2.0f;
        sensor.AddObservation(normalizedHeight);

        // 中心部分の回転をクォータニオンで取得
        foreach (var center in Center)
        {
            Quaternion rotation = center.transform.rotation;
            sensor.AddObservation(rotation.x);
            sensor.AddObservation(rotation.y);
            sensor.AddObservation(rotation.z);
            sensor.AddObservation(rotation.w);
        }

        // 腕の回転の正規化
        foreach (var arm in Arms)
        {
            Quaternion rotation = arm.transform.rotation;
            sensor.AddObservation(rotation.x);
            sensor.AddObservation(rotation.y);
            sensor.AddObservation(rotation.z);
            sensor.AddObservation(rotation.w);
        }

        // 脚の回転の正規化
        foreach (var leg in Legs)
        {
            Quaternion rotation = leg.transform.rotation;
            sensor.AddObservation(rotation.x);
            sensor.AddObservation(rotation.y);
            sensor.AddObservation(rotation.z);
            sensor.AddObservation(rotation.w);
        }

        sensor.AddObservation(hips.GetComponent<Rigidbody>().velocity.x); // グローバルな前進速度

        foreach (var joint in Legs_Joint)
        {
            sensor.AddObservation(joint.GetComponent<Rigidbody>().angularVelocity); // 各脚の関節速度
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
            float horizontalTorque = continuousActions[i]; // アクションバッファから値を取得
            ApplyTorqueToJoint(Center_Joint[i], horizontalTorque, Vector3.up); // 上方向のトルク
            float verticalTorque = continuousActions[i + 3];
            ApplyTorqueToJoint(Center_Joint[i], verticalTorque, Vector3.right);
        }

        // アームの動き
        for (int i = 0; i < 6; i++)
        {
            float horizontalTorque = continuousActions[i + 6]; // 水平トルク
            ApplyTorqueToJoint(Arml_Joint[i], horizontalTorque, Vector3.right); // 横方向

            float verticalTorque = continuousActions[i + 12]; // 垂直トルク
            ApplyTorqueToJoint(Arml_Joint[i], verticalTorque, Vector3.forward); // 前後方向
        }

        // 脚の動き
        for (int i = 0; i < 6; i++)
        {
            float horizontalTorque = continuousActions[i + 18]; // 水平トルク
            ApplyTorqueToJoint(Legs_Joint[i], horizontalTorque, Vector3.right);

            float verticalTorque = continuousActions[i + 24]; // 垂直トルク
            ApplyTorqueToJoint(Legs_Joint[i], verticalTorque, Vector3.forward);
        }

        float headposition = head.transform.position.y;
        for (int i = 0; i < Center.Length; i++)
        {
            currentcenterPosition += Center[i].transform.position[0];
        }
        // for (int i = 0; i < Legs.Length; i++)
        // {
        //     currentPosition += Legs[i].transform.position[0];
        // }

        currentcenterPosition += hips.transform.position.x;
        currentcenterPosition = currentcenterPosition / 4;
        float deltaX = currentcenterPosition - previouscenterPosition;
        previouscenterPosition = currentcenterPosition;

        AddReward(deltaX * 20f);
        if (deltaX < 0.05f)
        {
            AddReward(-0.1f);
        }

        if (head.transform.position.y > 1.0f) // 頭の高さが一定以上なら報酬
        {
            AddReward(0.5f);
        }
        else if (head.transform.position.y < 1.0f && head.transform.position.y > 0.5f)
        {
            AddReward(-0.1f);
        }
        else if (head.transform.position.y < 0.5f) // 転倒とみなす条件
        {
            // AddReward(-1.0f); // 大きなペナルティ
            EndEpisode();
        }

        if (hips.transform.position.y < initialHipsPosition.y - 0.2)
        {
            EndEpisode();
        }

       for (int i = 0; i < Legs.Length; i++)
        {
            float legDeltaX = currentlegPosition[i] - previouslegPosition[i];
            previouslegPosition[i] = currentlegPosition[i];

            if (legDeltaX > 0) // 前進している場合のみ報酬
            {
                AddReward(legDeltaX * 0.1f);
            }
            else // 後退した場合にはペナルティ（必要に応じて）
            {
                AddReward(legDeltaX * 0.05f);
            }
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
        Vector3 torqueVector = axis * torque * 150f; // 10fはスケーリング値で調整可能

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