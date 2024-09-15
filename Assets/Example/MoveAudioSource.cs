using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAudioSource : MonoBehaviour
{
	public float speed = 10, radius = 5;

	internal new Transform transform;
	private Vector3 pos;
	private float rot;

	private void Start()
	{
		transform = GetComponent<Transform>();
		pos = transform.position;
	}

	private void Update()
	{
		pos.x = Mathf.Sin(rot) * radius;
		transform.position = pos;
		rot += Time.deltaTime * speed;
	}

	private void OnGUI()
	{
		const int width = 512;
		GUI.Label(new Rect((Screen.width / 2) - (width / 2), (Screen.height / 2) - 50, width, 64), "Multiple audio listeners will each be able to hear audio at their own distance");
	}
}
