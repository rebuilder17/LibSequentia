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
	[SerializeField]
	UnityEngine.Audio.AudioMixer []	m_mixers_sec3;
	[SerializeField]
	UnityEngine.Audio.AudioMixer []	m_mixers_sec4;
	[SerializeField]
	UnityEngine.Audio.AudioMixer [] m_mixers_deckB;


	LibSequentiaAutomationManager	m_automationMgr;
	LibSequentiaAudioClipDepot		m_audioClipDepot;

	//TrackPlayer						m_trackplayer;
	MasterPlayer					m_masterplayer;

	float							m_tension	= 0;


	string[] autoctrlLayerNames	= { "da-1-1", "da-1-2", "da-1-3", "da-1-4", "da-2-1", "da-2-2", "da-2-3", "da-2-4", "db-1-1", "db-1-2", "db-1-3", "db-1-4", "db-2-1", "db-2-2", "db-2-3", "db-2-4", };
	string[] autoctrlSecNames	= { "da-1", "da-2", "db-1", "db-2" };

	Track []						m_tracks = new Track[2];
	int								m_trackIdx = 0;
	
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
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[8], m_mixers_sec3[0]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[9], m_mixers_sec3[1]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[10], m_mixers_sec3[2]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[11], m_mixers_sec3[3]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[12], m_mixers_sec4[0]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[13], m_mixers_sec4[1]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[14], m_mixers_sec4[2]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[15], m_mixers_sec4[3]);

		m_automationMgr.AddAutomationControlToMixer(autoctrlSecNames[0], m_mixers_deckA[0]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlSecNames[1], m_mixers_deckA[1]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlSecNames[2], m_mixers_deckB[0]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlSecNames[3], m_mixers_deckB[1]);

		// 텐션 오토메이션 버스 생성
		AutomationHub [] tensionCtrlBus			= new AutomationHub[4];
		IAutomationControl [] sec1TensionCtrls	= new IAutomationControl[4];
		IAutomationControl [] sec2TensionCtrls	= new IAutomationControl[4];
		for (int i = 0; i < 4; i++ )
		{
			var out1	= m_automationMgr.GetAutomationControlToSingleMixer(autoctrlLayerNames[i]);
			var out2	= m_automationMgr.GetAutomationControlToSingleMixer(autoctrlLayerNames[i + 4]);

			var bus		= new AutomationHub(m_automationMgr);
			bus.SetOutputs(out1, out2);
			bus.CreateChains(2);

			tensionCtrlBus[i]	= bus;

			sec1TensionCtrls[i]	= tensionCtrlBus[i].GetChain(0);
			sec2TensionCtrls[i]	= tensionCtrlBus[i].GetChain(1);
		}

		AutomationHub [] tensionCtrlBus2		= new AutomationHub[4];
		IAutomationControl [] sec3TensionCtrls	= new IAutomationControl[4];
		IAutomationControl [] sec4TensionCtrls	= new IAutomationControl[4];
		for (int i = 0; i < 4; i++ )
		{
			var out1	= m_automationMgr.GetAutomationControlToSingleMixer(autoctrlLayerNames[8 + i]);
			var out2	= m_automationMgr.GetAutomationControlToSingleMixer(autoctrlLayerNames[8 + i + 4]);

			var bus		= new AutomationHub(m_automationMgr);
			bus.SetOutputs(out1, out2);
			bus.CreateChains(2);

			tensionCtrlBus2[i]	= bus;

			sec3TensionCtrls[i]	= tensionCtrlBus2[i].GetChain(0);
			sec4TensionCtrls[i]	= tensionCtrlBus2[i].GetChain(1);
		}
		
		//

		m_audioClipDepot	= gameObject.AddComponent<LibSequentiaAudioClipDepot>();
		var clipPack		= m_audioClipDepot.LoadAndMakeAudioPack("test-sec1-1", "test-sec1-2", "test-sec1-3", "test-sec1-4", "test-sec2-1", "test-sec2-2", "test-sec2-3", "test-sec2-4");
		var clipPack2		= m_audioClipDepot.LoadAndMakeAudioPack("test2-sec1-1", "test2-sec1-2", "test2-sec1-3", "test2-sec1-4", "test2-sec2-1", "test2-sec2-2", "test2-sec2-3", "test2-sec2-4");

		m_tracks[0]			= Track.GenTestTrack(120, clipPack);
		m_tracks[1]			= Track.GenTestTrack2(120, clipPack2);

		var tplayer1		= new TrackPlayer(this);

		var player1ctrl		= m_automationMgr.GetAutomationControlToSingleMixer(autoctrlSecNames[0]);
		var player1			= CreateSectionPlayer(m_mixers_sec1, player1ctrl);
		player1.SetTensionAutomationTargets(sec1TensionCtrls);
		tplayer1.AttachSectionPlayer(player1, player1ctrl);

		var player2ctrl		= m_automationMgr.GetAutomationControlToSingleMixer(autoctrlSecNames[1]);
		var player2			= CreateSectionPlayer(m_mixers_sec2, player2ctrl);
		player2.SetTensionAutomationTargets(sec2TensionCtrls);
		tplayer1.AttachSectionPlayer(player2, player2ctrl);

		var tplayer2		= new TrackPlayer(this);

		var player3ctrl		= m_automationMgr.GetAutomationControlToSingleMixer(autoctrlSecNames[2]);
		var player3			= CreateSectionPlayer(m_mixers_sec3, player3ctrl);
		player3.SetTensionAutomationTargets(sec3TensionCtrls);
		tplayer2.AttachSectionPlayer(player3, player3ctrl);

		var player4ctrl		= m_automationMgr.GetAutomationControlToSingleMixer(autoctrlSecNames[3]);
		var player4			= CreateSectionPlayer(m_mixers_sec4, player4ctrl);
		player4.SetTensionAutomationTargets(sec4TensionCtrls);
		tplayer2.AttachSectionPlayer(player4, player4ctrl);

		m_masterplayer		= new MasterPlayer(this);
		m_masterplayer.SetTrackPlayers(tplayer1, tplayer2);
		m_masterplayer.SetNewTrack(m_tracks[0]);
		m_masterplayer.tension	= 0;
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
			m_masterplayer.DoNaturalProgress();
		}
		
		if (Input.GetKeyDown(KeyCode.M))			// 강제 진행
		{
			Debug.Log("Manual Transition");
			m_masterplayer.DoManualProgress();
		}

		if(Input.GetKeyDown(KeyCode.Equals))		// '+' 키 (텐션 업)
		{
			m_tension	= Mathf.Min(1f, m_tension + 0.1f);
			m_masterplayer.tension	= m_tension;
		}

		if (Input.GetKeyDown(KeyCode.Minus))		// '-' 키 (텐션 다운)
		{
			m_tension	= Mathf.Max(0f, m_tension - 0.1f);
			m_masterplayer.tension	= m_tension;
		}

		if (Input.GetKeyDown(KeyCode.Return))
		{
			m_trackIdx = (m_trackIdx + 1) % 2;
			m_masterplayer.SetNewTrack(m_tracks[m_trackIdx]);
		}
	}
}
