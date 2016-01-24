using UnityEngine;
using System.Collections;

public class BlockMotor : MonoBehaviour
{
	Transform							trans;

	public Vector3 RelativePos(Vector3 pos)
	{
		pos.Normalize(); //redundant-ish
		return trans.rotation*pos;
	}

	public Quaternion RelativeRotation(Quaternion rot)
	{
		return trans.rotation * rot; //XXX: check rhs and lhs
	}

	// Use this for initialization
	void Start ()
	{
		trans = this.gameObject.GetComponent<Transform>();
	}
	
	// Update is called once per frame
	void Update ()
	{
		//do some stuff
	}
}
