using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

using LibSequentia;


/// <summary>
/// LibSequentia 의 메인 컴포넌트.
/// </summary>
public class LibSequentiaMain : MonoBehaviour
{
	// Properties

	[SerializeField]
	AudioMixer		m_mixer_master;				// 마스터 오디오 믹서
	
	[SerializeField]
	AudioMixer		m_mixer_deckA;
	[SerializeField]
	AudioMixer []	m_mixer_deckALayers;

	[SerializeField]
	AudioMixer		m_mixer_deckB;
	[SerializeField]
	AudioMixer []	m_mixer_deckBLayers;


	// Members

	void Awake()
	{

	}
}
