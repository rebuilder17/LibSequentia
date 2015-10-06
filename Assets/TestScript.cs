using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

	[SerializeField]
	UnityEngine.Audio.AudioMixer [] m_mixers_decks;


	LibSequentiaAutomationManager	m_automationMgr;
	LibSequentiaAudioClipDepot		m_audioClipDepot;

	//TrackPlayer						m_trackplayer;
	MasterPlayer					m_masterplayer;

	float							m_tension	= 0;


	string[] autoctrlLayerNames	= { "da-1-1", "da-1-2", "da-1-3", "da-1-4", "da-2-1", "da-2-2", "da-2-3", "da-2-4", "db-1-1", "db-1-2", "db-1-3", "db-1-4", "db-2-1", "db-2-2", "db-2-3", "db-2-4", };
	string[] autoctrlSecNames	= { "da-1", "da-2", "db-1", "db-2" };
	string[] autoctrlDeckNames	= { "da", "db" };

	Track []						m_tracks = new Track[2];
	int								m_trackIdx = 0;
	TransitionScenario				m_tscen;
	
	void Awake()
	{
		// TEST : 버퍼 사이즈를 조절해본다.
		var audioSettings			= AudioSettings.GetConfiguration();
		Debug.Log("original buffer size is : " + audioSettings.dspBufferSize);
		audioSettings.dspBufferSize	= 2048;
		AudioSettings.Reset(audioSettings);
		//

		m_automationMgr = gameObject.AddComponent<LibSequentiaAutomationManager>();
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[0], m_mixers_sec1[0], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[1], m_mixers_sec1[1], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[2], m_mixers_sec1[2], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[3], m_mixers_sec1[3], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[4], m_mixers_sec2[0], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[5], m_mixers_sec2[1], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[6], m_mixers_sec2[2], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[7], m_mixers_sec2[3], true);

		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[8], m_mixers_sec3[0], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[9], m_mixers_sec3[1], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[10], m_mixers_sec3[2], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[11], m_mixers_sec3[3], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[12], m_mixers_sec4[0], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[13], m_mixers_sec4[1], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[14], m_mixers_sec4[2], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlLayerNames[15], m_mixers_sec4[3], true);

		m_automationMgr.AddAutomationControlToMixer(autoctrlSecNames[0], m_mixers_deckA[0], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlSecNames[1], m_mixers_deckA[1], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlSecNames[2], m_mixers_deckB[0], true);
		m_automationMgr.AddAutomationControlToMixer(autoctrlSecNames[3], m_mixers_deckB[1], true);

		m_automationMgr.AddAutomationControlToMixer(autoctrlDeckNames[0], m_mixers_decks[0]);
		m_automationMgr.AddAutomationControlToMixer(autoctrlDeckNames[1], m_mixers_decks[1]);


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
		
		var track1json		= new JSONObject(Resources.Load<TextAsset>("data/track1").text);
		var clipPack		= m_audioClipDepot.LoadAndMakeAudioPack(Track.GatherRequiredClips(track1json));
		m_tracks[0]			= Track.CreateFromJSON(track1json, clipPack);

		var track2json		= new JSONObject(Resources.Load<TextAsset>("data/track2").text);
		var clipPack2		= m_audioClipDepot.LoadAndMakeAudioPack(Track.GatherRequiredClips(track2json));
		m_tracks[1]			= Track.CreateFromJSON(track2json, clipPack2);

		var tscenjson		= new JSONObject(Resources.Load<TextAsset>("data/ts_crossfade").text);
		m_tscen				= TransitionScenario.CreateFromJSON(tscenjson);

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
		var deckActrl		= m_automationMgr.GetAutomationControlToSingleMixer(autoctrlDeckNames[0]);
		var deckBctrl		= m_automationMgr.GetAutomationControlToSingleMixer(autoctrlDeckNames[1]);
		m_masterplayer.SetTransitionCtrls(deckActrl, deckBctrl);
		
		//m_masterplayer.SetNewTrack(m_tracks[m_trackIdx], null);
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



	// Step 이동 테스트
	struct StepState
	{
		public Track	curtrack;
		public int		step;

		public Track	newtrack;
		public int		newstep;

		public TransitionScenario tscen;
	}

	List<StepState>	m_stateSeq	= new List<StepState>();
	//StepState		m_curstate	= new StepState();
	int				m_stateidx	= 0;
	StepControl		m_ctrl;

	void NextStep()
	{
		if (m_stateidx == 0)
		{
			m_ctrl.StartWithOneTrack(m_stateSeq[0].curtrack, 1, false);
		}
		else if (m_stateidx >= m_stateSeq.Count)
		{
			m_ctrl.StepMove(m_stateSeq[m_stateSeq.Count - 1].step + 1, false);
		}
		else
		{
			var prev	= m_stateSeq[m_stateidx - 1];
			var cur		= m_stateSeq[m_stateidx];
			if (cur.newtrack != null && prev.newtrack == null)
			{
				Debug.LogWarning("newtrack!");
				m_ctrl.StepMove(cur.step, cur.newtrack, cur.tscen, false);
			}
			else if (cur.newtrack != null && prev.newtrack != null)
			{
				if (cur.newstep >= 2)	// curstep / newstep 부분의 전환 판단 (새 트랙은 새 step으로 진행시켜줘야함)
				{
					m_ctrl.StepMove(cur.newstep, false);
				}
				else
				{
					m_ctrl.StepMove(cur.step, false);
				}
			}
	
			else
			{
				m_ctrl.StepMove(cur.step, false);
			}
		}

		if (m_stateidx < m_stateSeq.Count)
		{
			m_stateidx++;
		}
	}

	void PrevStep()
	{
		if (m_stateidx > 0)
		{
			m_stateidx--;
		}
		

		if (m_stateidx >= m_stateSeq.Count - 1)
		{
			var last	= m_stateSeq[m_stateSeq.Count - 1];
			m_ctrl.StartWithOneTrack(last.curtrack, last.step, true);
		}
		else if (m_stateidx < 0)
		{
			m_ctrl.StepMove(0, true);
		}
		else
		{
			var prev	= m_stateSeq[m_stateidx + 1];
			var cur		= m_stateSeq[m_stateidx];
			if (cur.newtrack != null && prev.newtrack == null)
			{
				Debug.LogWarning("newtrack! (reverse)");
				m_ctrl.StepMove(cur.newstep, cur.curtrack, cur.tscen, true);
			}
			else if (cur.newtrack != null && prev.newtrack != null)
			{
				if (cur.step <= cur.curtrack.sectionCount * 2)	// curstep / newstep 부분의 전환 판단 (새 트랙은 새 step으로 진행시켜줘야함)
				{
					m_ctrl.StepMove(cur.step, true);
				}
				else
				{
					m_ctrl.StepMove(cur.newstep, true);
				}
			}
	
			else
			{
				m_ctrl.StepMove(cur.step, true);
			}
		}
	}



	void Start()
	{
		var track1	= m_tracks[0];
		var track2	= m_tracks[1];

		m_stateSeq.Add(new StepState() { curtrack = track1, step = 1 });
		m_stateSeq.Add(new StepState() { curtrack = track1, step = 2 });
		m_stateSeq.Add(new StepState() { curtrack = track1, step = 3 });
		m_stateSeq.Add(new StepState() { curtrack = track1, step = 4 });
		m_stateSeq.Add(new StepState() { curtrack = track1, step = 5, newtrack = track2, newstep = 1, tscen = m_tscen });
		m_stateSeq.Add(new StepState() { curtrack = track1, step = 6, newtrack = track2, newstep = 2, tscen = m_tscen });
		m_stateSeq.Add(new StepState() { curtrack = track1, step = 7, newtrack = track2, newstep = 3, tscen = m_tscen });
		m_stateSeq.Add(new StepState() { curtrack = track1, step = 4 });
		m_stateSeq.Add(new StepState() { curtrack = track2, step = 5 });
		m_stateSeq.Add(new StepState() { curtrack = track2, step = 6 });
		m_stateSeq.Add(new StepState() { curtrack = track2, step = 7 });

		m_ctrl	= new StepControl(m_masterplayer, this);
	}
	

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Quote))
		{
			NextStep();
		}

		if (Input.GetKeyDown(KeyCode.Semicolon))
		{
			if (m_stateidx == 0)
				m_stateidx = m_stateSeq.Count;

			PrevStep();
		}


		/*
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


		if (Input.GetKeyDown(KeyCode.C))			// C 키 : 다음 트랙 준비
		{
			Debug.Log("newtrack");
			m_trackIdx = (m_trackIdx + 1) % 2;
			m_masterplayer.SetNewTrack(m_tracks[m_trackIdx], m_tscen);
		}
		 */

		
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

		if (Input.GetKeyDown(KeyCode.Comma))		// < 키 (이전 트랙쪽으로 트랜지션 옮기기)
		{
			m_masterplayer.transition	= Mathf.Max(0, m_masterplayer.transition - 0.1f);
		}

		if (Input.GetKeyDown(KeyCode.Period))		// > 키 (다음 트랙쪽으로 트랜지션 옮기기)
		{
			m_masterplayer.transition	= Mathf.Min(1, m_masterplayer.transition + 0.1f);
		}
		 
	}

	void OnGUI()
	{
		/*
		var buttonrect	= new Rect() { x = 0, y = 0, width = 200, height = 100 };
		if (GUI.Button(buttonrect, "Natural"))			// 자연 진행
		{
			Debug.Log("Natural Transition");
			m_masterplayer.DoNaturalProgress();
		}

		buttonrect.x	= 200;
		if (GUI.Button(buttonrect, "Manual"))			// 강제 진행
		{
			Debug.Log("Manual Transition");
			m_masterplayer.DoManualProgress();
		}

		buttonrect.x	= 0;
		buttonrect.y	= 100;
		if (GUI.Button(buttonrect, "tension+"))		// '+' 키 (텐션 업)
		{
			m_tension	= Mathf.Min(1f, m_tension + 0.1f);
			m_masterplayer.tension	= m_tension;
		}

		buttonrect.x	= 200;
		if (GUI.Button(buttonrect, "tension-"))		// '-' 키 (텐션 다운)
		{
			m_tension	= Mathf.Max(0f, m_tension - 0.1f);
			m_masterplayer.tension	= m_tension;
		}

		buttonrect.x	= 0;
		buttonrect.y	= 200;
		if (GUI.Button(buttonrect, "NextTrack"))			// C 키 : 다음 트랙 준비
		{
			Debug.Log("newtrack");
			m_trackIdx = (m_trackIdx + 1) % 2;
			m_masterplayer.SetNewTrack(m_tracks[m_trackIdx], m_tscen);
		}

		buttonrect.x	= 0;
		buttonrect.y	= 300;
		if (GUI.Button(buttonrect, "<<"))		// < 키 (이전 트랙쪽으로 트랜지션 옮기기)
		{
			m_masterplayer.transition	= Mathf.Max(0, m_masterplayer.transition - 0.1f);
		}

		buttonrect.x	= 200;
		if (GUI.Button(buttonrect, ">>"))		// > 키 (다음 트랙쪽으로 트랜지션 옮기기)
		{
			m_masterplayer.transition	= Mathf.Min(1, m_masterplayer.transition + 0.1f);
		}
		 */
	}
}
