using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using LibSequentia.Data;
using LibSequentia.Engine;

public class LibSequentiaAutomationManager : MonoBehaviour, IAutomationHubManager
{
	/// <summary>
	/// Audio Mixer를 대상으로 한 Automation 컨트롤
	/// </summary>
	class AudioMixerAutomationControl : IAutomationControl
	{
		// Static Members

		class ParamInfo
		{
			public delegate float ValueFunc(float input, ParamInfo info);

			public float	valueMin;			// 입력값이 0일 때 mixer 파라미터 값
			public float	valueMax;			// 입력값이 1일 때 mixer 파라미터 값
			public string	mixerParamName;		// mixer external parameter 이름
			public ValueFunc valueFunc;			// 인풋-아웃풋 변환 함수

			public float ToMixerParamValue(float value)
			{
				//return (valueMax - valueMin) * value + valueMin;
				return valueFunc(value, this);
			}
		}

		static Dictionary<Automation.TargetParam, ParamInfo>	s_infoDict;

		static AudioMixerAutomationControl()
		{
			// 파라미터 정보 채우기
			s_infoDict	= new Dictionary<Automation.TargetParam, ParamInfo>();

			// 기본값은 null으로 둔다
			int paramCount	= Automation.targetParamEnumValues.Length;
			for(int i = 0; i < paramCount; i++)
			{
				s_infoDict[Automation.targetParamEnumValues[i]]	= null;
			}

			// 변환 함수 목록
			ParamInfo.ValueFunc func_linear	= (float input, ParamInfo info) => (info.valueMax - info.valueMin) * input - info.valueMin;
			ParamInfo.ValueFunc func_volume	= (float input, ParamInfo info) => Mathf.Max(info.valueMin, 20 * Mathf.Log10(input));

			//
			s_infoDict[Automation.TargetParam.Volume]	= new ParamInfo() { valueMin = -80f,	valueMax = 0f,	mixerParamName = "Volume",	valueFunc = func_volume };
		}


		// Members

		AudioMixer	m_mixer;

		public AudioMixerAutomationControl(AudioMixer mixer)
		{
			m_mixer	= mixer;
		}

		public void Set(Automation.TargetParam param, float value)
		{
			var info	= s_infoDict[param];
			m_mixer.SetFloat(info.mixerParamName, info.ToMixerParamValue(value));
		}
	}
	//



	// Members

	Dictionary<string, AudioMixerAutomationControl>	m_mixerControls	= new Dictionary<string, AudioMixerAutomationControl>();

	List<IAutomationHubHandle>	m_handles	= new List<IAutomationHubHandle>();	// 업데이트해줘야할 automation hub 핸들


	void Awake()
	{
	}

	void Update()
	{
		int count	= m_handles.Count;
		for(int i = 0; i < count;i++)
		{
			m_handles[i].Update();
		}
	}

	public void AddAutomationControlToMixer(string ctrlname, AudioMixer mixer)
	{
		m_mixerControls[ctrlname]	= new AudioMixerAutomationControl(mixer);
	}

	public IAutomationControl GetAutomationControlToSingleMixer(string ctrlname)
	{
		return m_mixerControls[ctrlname];
	}

	public void RegisterAutomationHub(IAutomationHubHandle hub)
	{
		m_handles.Add(hub);
	}
}
