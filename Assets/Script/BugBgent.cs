using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BugAgent : Agent
{
    public Transform target;
    public GameObject body;
    public GameObject[] thighs = new GameObject[4];
    public GameObject[] legs = new GameObject[4]; 
    public HingeJoint[] thighs_hinge = new HingeJoint[4];
    public HingeJoint[] legs_hinge = new HingeJoint[4];
    private JointMotor[] thighs_motor = new JointMotor[4];
    private JointMotor[] legs_motor = new JointMotor[4];
    private int phase = 0;
    private float rotate_phase = 1.0f;

    public override void Initialize(){
        // this.body = this.transform.Find("body").gameObject;
        // this.thighs[0] = this.body.transform.Find("L-1").gameObject;
        // this.thighs[1] = this.body.transform.Find("L_2").gameObject;
        // this.thighs[2] = this.body.transform.Find("R_1").gameObject;
        // this.thighs[3] = this.body.transform.Find("R_2").gameObject;
        int limit = 30;
        for(int i=0;i<4;i++){
            // this.legs[i] = this.thighs[i].transform.Find("leg").gameObject;
            this.thighs_hinge[i] = thighs[i].GetComponent<HingeJoint>();
            this.legs_hinge[i] = legs[i].GetComponent<HingeJoint>();
            this.thighs_motor[i] = this.thighs_hinge[i].motor;
            this.legs_motor[i] = this.legs_hinge[i].motor;
            JointLimits thigh_limits = thighs_hinge[i].limits;
            JointLimits leg_limits = legs_hinge[i].limits;
            thigh_limits.min = -limit;
            thigh_limits.max = limit;
            thigh_limits.bounciness = 0;
            thigh_limits.bounceMinVelocity = 0;
            thighs_hinge[i].limits = thigh_limits;
            thighs_hinge[i].useLimits = true;
            leg_limits.min = -limit;
            leg_limits.max = limit;
            leg_limits.bounciness = 0;
            leg_limits.bounceMinVelocity = 0;
            legs_hinge[i].limits = leg_limits;
            legs_hinge[i].useLimits = true;
        }
    }

    public override void OnEpisodeBegin(){
        this.body.transform.localPosition = new Vector3(0,1.3f,-10);
        this.body.transform.localRotation = Quaternion.Euler(90, 0, 0);
        for(int i=0;i<4;i++){
            this.thighs[i].transform.localRotation = Quaternion.Euler(-90, 0, 0);
            this.legs[i].transform.localRotation = Quaternion.Euler(0,0,0);
            this.thighs_motor[i].targetVelocity = 0;
            this.thighs_hinge[i].motor = this.thighs_motor[i];
            this.legs_motor[i].targetVelocity = 0;
            this.legs_hinge[i].motor = this.legs_motor[i];
        }
        this.phase = 0;
        this.rotate_phase = 1.0f;
        target.localPosition = new Vector3(0, 1.0f, 10.0f);
    }

    public override void CollectObservations(VectorSensor sensor){
        sensor.AddObservation(target.localPosition);
        sensor.AddObservation(this.body.transform.localPosition);
        sensor.AddObservation(this.body.transform.localRotation);
        sensor.AddObservation(this.body.transform.up.z);
        sensor.AddObservation(this.body.transform.forward.y);
        for(int i=0;i<4;i++){
            sensor.AddObservation(this.thighs[i].transform.localRotation.x);
            sensor.AddObservation(this.legs[i].transform.localRotation.x);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers){
        float force = 1500.0f;
        var continuousActions = actionBuffers.ContinuousActions;
        for(int i=0;i<4;i++){
            this.thighs_motor[i].targetVelocity = continuousActions[i]*force;
            this.legs_motor[i].targetVelocity = continuousActions[i+4]*force;
            this.thighs_hinge[i].motor = this.thighs_motor[i];
            this.legs_hinge[i].motor = this.legs_motor[i];
        }
        AddReward(-0.001f);
        float distanceToTarget = Vector3.Distance(this.body.transform.localPosition, target.localPosition);
        if (distanceToTarget < 18.0f - this.phase && this.phase < 15){
            AddReward(0.05f);
            this.phase += 1;
        }
        if (distanceToTarget < 2.0f){
            AddReward(2.0f);
            EndEpisode();
        }

        Vector3 rotate_angle = this.body.transform.localRotation.eulerAngles;
        float rotation = this.body.transform.up.z;
        bool is_rotate = rotation < Math.Cos(Math.PI/6.0f * rotate_phase);
        bool is_flip_z = this.body.transform.forward.y > 0;
        if (this.body.transform.localPosition.y < 0.0f || is_flip_z || rotation < Math.Cos(Math.PI/6.0f * 3.0f)){
            EndEpisode();
        }
        if(is_rotate && rotate_phase < 2){
            AddReward(-0.1f);
            rotate_phase += 1.0f;
        }
    }
    
    // public override void Heuristic(in ActionBuffers actionsOut){
    //     var continuousActions = actionsOut.ContinuousActions;
    //     actionsOut[0] = Input.GetAxis("Horizontal");
    //     actionsOut[1] = Input.GetAxis("Vertical");
    // }

    public void Update(){
    }
}

