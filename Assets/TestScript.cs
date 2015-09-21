using UnityEngine;
using System.Collections;

using LibSequentia.Engine;
using LibSequentia.Data;

public class TestScript : MonoBehaviour
{
	[SerializeField]
	UnityEngine.Audio.AudioMixer []	m_mixers_sec1;
	[SerializeField]
	UnityEngine.Audio.AudioMixer []	m_mixers_sec2;
	[SerializeField]
	UnityEngine.Audio.AudioMixer [] m_mixers_deckA;


	LibSequentiaAutomationManager	m_automationMgr;
	LibSequentiaAudioClipDepot		m_audioClipDepot;

	TrackPlayer						m_trackplayer;


	string[] autoctrlLayerNames	= { "da-1-1", "da-1-2", "da-1-3", "da-1-4", "da-2-1", "da-2-2", "da-2-3", "da-2-4", };
	string[] autoctrlSecNames	= { "da-1", "da-2" };
	
	void Awake()
	{
		m_automationMgr = gameObject.AddComponent<LibSequentiaAutomationManager>();
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[0], m_mixers_sec1[0]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[1], m_mixers_sec1[1]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[2], m_mixers_sec1[2]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[3], m_mixers_sec1[3]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[4], m_mixers_sec2[0]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[5], m_mixers_sec2[1]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[6], m_mixers_sec2[2]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[7], m_mixers_sec2[3]);

		m_automationMgr.AddAutomationControlToMixer(autoctrlSecNames[0], m_mixers_deckA[0]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlSecNames[1], m_mixers_deckA[1]);

		//

		m_audioClipDepot	= gameObject.AddComponent<LibSequentiaAudioClipDepot>();
		var clipPack		= m_audioClipDepot.LoadAndMakeAudioPack("test-sec1-1", "test-sec1-2", "test-sec1-3", "test-sec1-4", "test-sec2-1", "test-sec2-2", "test-sec2-3", "test-sec2-4");

		m_trackplayer		= new TrackPlayer(this);

		var player1ctrl		= m_automationMgr.GetAutomationControlToSingleMixer(autoctrlSecNames[0]);
		var player1			= CreateSectionPlayer(m_mixers_sec1, player1ctrl);
		m_trackplayer.AttachSectionPlayer(player1, player1ctrl);

		var player2ctrl		= m_automationMgr.GetAutomationControlToSingleMixer(autoctrlSecNames[1]);
		var player2			= CreateSectionPlayer(m_mixers_sec2, player2ctrl);
		m_trackplayer.AttachSectionPlayer(player2, player2ctrl);

		m_trackplayer.SetTrack(Track.GenTestTrack(120, clipPack));
	}

	SectionPlayer CreateSectionPlayer(UnityEngine.Audio.AudioMixer [] layermixers, IAutomationControl sectionMixerCtrl)
	{
		var player1		= gameObject.AddComponent<LibSequentiaPlayer>();
		player1.SetTargetMixers(layermixers);

		var player2		= gameObject.AddComponent<LibSequentiaPlayer>();
		player2.SetTargetMixers(layermixers);

		var secPlayer	= new SectionPlayer(this);
		secPlayer.SetPlayerComponents(player1, player2);
		secPlayer.SetTransitionAutomationTarget(sectionMixerCtrl);

		return secPlayer;
	}



	void Start()
	{
		//m_clock	= new BeatSyncClock(m_track.BPM);
	}
	

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.N))			// 자연 진행
		{
			Debug.Log("Natural Transition");
			m_trackplayer.DoNaturalProgress();
		}
		
		if (Input.GetKeyDown(KeyCode.M))			// 강제 진행
		{
			Debug.Log("Manual Transition");
			m_trackplayer.DoManualProgress();
		}
	}
}
