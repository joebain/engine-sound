using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class EngineSoundGenerator : MonoBehaviour {

	private ISoundsLikeACar car;

	private AudioClip bangClip;

	private AudioSource engineSource;

	private float[] bangSamples;
	private float outputSampleRate;
	private int bangClipSampleLength;

	public int MaxOverlaps = 10;

	public AnimationCurve soundTaper;

    private bool carWasStopped = false;

    // Use this for initialization
    void Start () {
		car = GetComponentInParent<ISoundsLikeACar>();
		AudioSource[] audioSources = GetComponents<AudioSource>();
		engineSource = audioSources[0];

		if (bangClip == null) {
			bangClip = engineSource.clip;
		} else {
			engineSource.clip = bangClip;
		}

		outputSampleRate = AudioSettings.outputSampleRate;

		bangClipSampleLength = bangClip.samples;
		bangSamples = new float[bangClip.samples];
		bangClip.GetData(bangSamples, 0);
	}

	void Update (){
		if (Input.GetKeyDown("s")) {
			engineSource.Play();
		}

        if (carWasStopped && !car.AllStopped)
        {
            engineSource.Play();
            carWasStopped = false;
        }
    }
	
	public VariationPair[] variations;
	
	private float bangPos, lastBangInterval, thisBangInterval, bangInterval, bangsPS, x, bangIntervalRemainder, samplePos, samplePosRemainder;
	private int bangIntervalQuotient, bangOverlaps, i, o, c, samplePosQuotient;
	private float lastBufferBangOffset = 0;
	private float iv0 = 0, iv1 = 0, iv2 = 0;
	void OnAudioFilterRead(float[] data, int channels) {
		if (bangSamples == null) return;

		if (car.AllStopped) {
			for (i = 0 ; i < data.Length; i++) {
				data[i] = 0;
			}
            carWasStopped = true;
            return;
		}

		// we want to add samples to our buffer, we have a previous frequency and a current frequency
		// at which we want to add the samples. so when we start writing to a new part of the buffer
		// we need to make sure the ends of the old samples are written. this means the first n samples
		// in the output buffer must feature the creeping end of the bang sample. And the offset into
		// the bang sample should be derived from the previous start and end bang intervals
		// for the current buffer we are going to leave iv1 interval between the first and second, then
		// iv1`, iv1``, until we get to iv2. The difference between each iv will average (iv1+iv2)/2 (iva)
		// and there will be bufLen/iva total samples (S). So between each iv we need to increase by
		// (iv2-iv1)*(buflen/iva).
		/*
		bangsPS = (car.EngineRPM * (car.PistonCount/2f)) / 60f;

		iv0 = iv1;
		iv1 = iv2;
		iv2 = outputSampleRate / bangsPS;

		float iva = (iv1+iv2)/2;
		int bufferLength = data.Length/channels;
		float S = bufferLength / iva;
		//Debug.Log("interval rate " + iv2);
		//Debug.Log("number of bangs in this buffer " + S);
		//Debug.Log("bangs per sec: " + bangsPS);
		float ivStep = (iv2-iv1)/S;


		float thisBufferBangOffset = lastBufferBangOffset;
		//Debug.Log ("buffer offset " + thisBufferBangOffset);

		for (int s = 0 ; s < S ; s++) {
			float bangStart = thisBufferBangOffset + (iv1+ivStep*s)*s;

			//lastBufferBangOffset = (bangStart + bangClipSampleLength) % bufferLength;
			int bangStartQuotient = Mathf.FloorToInt(bangStart);
			//float bangStartRemainder = bangStart-bangStartQuotient;
			//Debug.Log ("buffer write start " + bangStartQuotient);
			for (int b = 0 ; b < bangClipSampleLength ; b++) {
				for (c = 0 ; c < channels ; c++) {
					int d0 = (bangStartQuotient + b)*channels + c;
					int d1 = d0+channels+c;
					if (d1 >= data.Length) break;
					data[d0] += bangSamples[b];//*bangStartRemainder*soundTaper.Evaluate(s/S);
					//data[d1] += bangSamples[b]*(1-bangStartRemainder)*soundTaper.Evaluate(s/S);
				}
			}
		}
		*/

		/*
		bangInterval = outputSampleRate / bangsPS;
		bangIntervalQuotient = Mathf.FloorToInt(bangInterval);
		bangIntervalRemainder = bangInterval - bangIntervalQuotient;
		//bangOverlaps = Mathf.Min(Mathf.FloorToInt(bufferLength / bangIntervalQuotient), MaxOverlaps);
		bangOverlaps = Mathf.Min(Mathf.FloorToInt(bangClipSampleLength / bangIntervalQuotient), MaxOverlaps);
		for (i = 0; i < data.Length; i = i + channels) {
			for (c = 0 ; c < channels ; c++) {
				data[i+c] = 0;
				for (o = 0 ; o < bangOverlaps ; o++) {
					samplePos = (bangPos + o*bangInterval) % (bangClipSampleLength-1);
					samplePosQuotient = Mathf.FloorToInt(samplePos);
					samplePosRemainder = samplePos - samplePosQuotient;
					x = bangSamples[samplePosQuotient]*samplePosRemainder + bangSamples[samplePosQuotient+1]*(1f-samplePosRemainder);
					data[i+c] += x*soundTaper.Evaluate(o/bangOverlaps);
				}
			}
			bangPos = (bangPos + 1) % bangInterval;
		}
		*/

		// all this shit is wrong. the first attempt is less wrong though. since we are often going to have less than
		// one sample per buffer, max maybe 4 starting, we are not going to save much or anything by keeping track of buffer offsets
		// we may as well just keep track of sample playback individually. so we get a 100 long circular buffer (or however many
		// concurrent samples we want to play) and we start each one with an interval based on the current engine rpm.
		// We have to check the current next-to-finish sample and when it finishes we can stop it
		// in some cases (maybe most cases) the samples won't finish before we have to replace them so we don't need to worry.
		// so if we have 100 samples playing and we are firing at max 9000rpm, which is 450 per second, so each sample will
		// only get to play for 1/4 second, which is ok, maybe a little short but probably hard to detect with that many going off
		// we probably want a sharper tail on the bang to make the peaks in the sound more obvious
		// if we can go to 0.5secs, so 200 samples, we are making gravy. lets try that
		bangsPS = (car.EngineRPM * (car.PistonCount/2f)) / (60f);
		bangInterval = outputSampleRate / bangsPS;
		bangIntervalQuotient = Mathf.RoundToInt(bangInterval);
		bangIntervalRemainder = bangInterval - bangIntervalQuotient;
		uint outBufferSamples = (uint)(data.Length/channels);
		while (samplesSinceLastBang > bangInterval) {
			samplesSinceLastBang -= bangInterval;

			samplePositionsIndex = (samplePositionsIndex + 1) % samplePositions.Length;
			samplePositions[samplePositionsIndex] = (uint)samplesSinceLastBangQuotient;
		}
		samplesSinceLastBangQuotient = Mathf.FloorToInt(samplesSinceLastBang);
		samplesSinceLastBangRemainder = samplesSinceLastBang-samplesSinceLastBangQuotient;

		int sampleIteratorI = samplePositionsIndex;
		while (samplePositions[sampleIteratorI] > 0) {
			uint sampleIndex = samplePositions[sampleIteratorI];
			int pistonIndex = sampleIteratorI%car.PistonCount;
			float variationMod = 1;
			for (int v = 0 ; v < variations.Length ; v++) {
				variationMod += variationMod*Mathf.Sin(sampleIteratorI/(variations[v].frequency*Mathf.PI))*variations[v].value;
			}
			for (int d = 0 ; d < data.Length && sampleIndex < bangSamples.Length-1 ; d += channels, sampleIndex++) {
				for (int c = 0 ; c < channels ; c++) {
					float value = (bangSamples[sampleIndex]*samplesSinceLastBangRemainder + bangSamples[sampleIndex+1]*(1f-samplesSinceLastBangRemainder))*pistonVolumes[pistonIndex];
					value += value*variationMod;
					data[d+c] += value;
				}
			}
			samplePositions[sampleIteratorI] = sampleIndex;
			if (samplePositions[sampleIteratorI] >= bangSamples.Length-1) {
				samplePositions[sampleIteratorI] = 0;
			}
			sampleIteratorI = ((sampleIteratorI - 1) + samplePositions.Length) % samplePositions.Length;
		}
		for (int d = 0 ; d < data.Length; d ++) {
			data[d] *= engineVolume;
			data[d] = Mathf.Clamp01(data[d]);
		}

		samplesSinceLastBang += outBufferSamples;
	}
	public float engineVolume = 0.05f;
	float[] pistonVolumes = new float[]{0.3f, 0.6f, 0.2f, 0.8f, 0.4f, 0.7f, 0.2f, 0.8f};
	float samplesSinceLastBang = 0;
	int samplesSinceLastBangQuotient = 0;
	float samplesSinceLastBangRemainder = 0;
	int samplePositionsIndex = 0;
	private uint[] samplePositions = new uint[100];
}

[System.Serializable]
public class VariationPair {
	public float frequency = 1;
	public float value = 1;
}
