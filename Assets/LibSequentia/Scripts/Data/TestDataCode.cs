using UnityEngine;
using System.Collections;


namespace LibSequentia.Data
{
	public partial class Layer
	{
		public static Layer GenTestLayer(IAudioClipHandle handle)
		{
			var layer	= new Layer();
			layer.clipHandle	= handle;

			return layer;
		}
	}

	public partial class Section
	{
		public static Section GenTestSection1(IAudioClipPack clipPack)
		{
			var section	= new Section();

			// BPM:120, 4/4 -> 1bar = 4beat

			var layer1			= Layer.GenTestLayer(clipPack.GetHandle("test-sec1-1"));
			section.m_layers[0]	= layer1;

			var layer2			= Layer.GenTestLayer(clipPack.GetHandle("test-sec1-2"));
			section.m_layers[1]	= layer2;

			var layer3			= Layer.GenTestLayer(clipPack.GetHandle("test-sec1-3"));
			section.m_layers[2]	= layer3;

			var layer4			= Layer.GenTestLayer(clipPack.GetHandle("test-sec1-4"));
			section.m_layers[3]	= layer4;

			section.inTypeNatural	= InType.KickIn;
			section.inTypeManual	= InType.FadeIn;
			section.outTypeNatural	= OutType.LeaveIt;
			section.outTypeManual	= OutType.FadeOut;
			section.doNotOverlapFillIn	= false;

			section.beatFillIn		= 0;
			section.beatStart		= 1 * 4;
			section.beatEnd			= 9 * 4;

			return section;
		}

		public static Section GenTestSection2(IAudioClipPack clipPack)
		{
			var section	= new Section();

			// BPM:120, 4/4 -> 1bar = 4beat

			var layer1			= Layer.GenTestLayer(clipPack.GetHandle("test-sec2-1"));
			section.m_layers[0]	= layer1;

			var layer2			= Layer.GenTestLayer(clipPack.GetHandle("test-sec2-2"));
			section.m_layers[1]	= layer2;

			var layer3			= Layer.GenTestLayer(clipPack.GetHandle("test-sec2-3"));
			section.m_layers[2]	= layer3;

			var layer4			= Layer.GenTestLayer(clipPack.GetHandle("test-sec2-4"));
			section.m_layers[3]	= layer4;

			section.inTypeNatural	= InType.KickIn;
			section.inTypeManual	= InType.FadeIn;
			section.outTypeNatural	= OutType.LeaveIt;
			section.outTypeManual	= OutType.FadeOut;
			section.doNotOverlapFillIn	= false;

			section.beatFillIn		= 0;
			section.beatStart		= 1 * 4;
			section.beatEnd			= 9 * 4;

			return section;
		}
	}

	public partial class Track
	{
		public static Track GenTestTrack(float bpm, IAudioClipPack clipPack)
		{
			var track			= new Track();

			track.BPM			= bpm;

			var section1		= Section.GenTestSection1(clipPack);
			var section2		= Section.GenTestSection2(clipPack);

			track.m_sectionSeq.Add(section1);
			track.m_sectionSeq.Add(section2);
			track.m_sectionSeq.Add(section1);
			
			return track;
		}
	}
}