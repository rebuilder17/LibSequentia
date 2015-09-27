using UnityEngine;
using System.Collections;

using LibSequentia.Data;

public class TestScript2 : MonoBehaviour
{

	// Use this for initialization
	void Start()
	{
		var intro_vol			= new Automation();
		intro_vol.targetParam	= Automation.TargetParam.Volume;
		intro_vol.AddPoint(0, 0);
		intro_vol.AddPoint(0.5f, 1);
		intro_vol.AddPoint(1.0f, 1);

		Output(intro_vol);


		var intro_lowcut		= new Automation();
		intro_lowcut.targetParam	= Automation.TargetParam.LowCut;
		intro_lowcut.AddPoint(0, 1);
		intro_lowcut.AddPoint(0.5f, 1);
		intro_lowcut.AddPoint(0.5f, 0);
		intro_lowcut.AddPoint(1, 0);

		Output(intro_lowcut);
	}

	void Output(Automation auto)
	{
		string output = "";
		for (int i = 0; i < 11; i++)
		{
			output += string.Format("{0}, ", auto.GetValue((float)i * 0.1f));
		}
		Debug.Log(output);
	}
}
