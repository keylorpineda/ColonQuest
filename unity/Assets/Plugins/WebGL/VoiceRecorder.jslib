// Records one negotiation clip in the browser (Unity's Microphone does not work in WebGL).
// getUserMedia feeds a ScriptProcessor that keeps mono float samples; on stop, or after the
// time limit, they are resampled to 16 kHz and packed as a 16-bit PCM WAV. C# polls the state
// and copies the bytes with VoiceRecorder_CopyResult. States: 0 idle, 1 starting, 2 recording,
// 3 ready, 4 failed.
var VoiceRecorderLib = {
  $VoiceRec: {
    state: 0,
    stream: null,
    context: null,
    source: null,
    processor: null,
    chunks: [],
    length: 0,
    rate: 0,
    maxSeconds: 15,
    wav: null
  },

  $VoiceRecRelease__deps: ['$VoiceRec'],
  $VoiceRecRelease: function () {
    if (VoiceRec.processor) {
      VoiceRec.processor.onaudioprocess = null;
      try { VoiceRec.processor.disconnect(); } catch (e) {}
    }
    if (VoiceRec.source) {
      try { VoiceRec.source.disconnect(); } catch (e) {}
    }
    if (VoiceRec.stream) {
      VoiceRec.stream.getTracks().forEach(function (track) { track.stop(); });
    }
    if (VoiceRec.context) {
      try { VoiceRec.context.close(); } catch (e) {}
    }
    VoiceRec.stream = null;
    VoiceRec.context = null;
    VoiceRec.source = null;
    VoiceRec.processor = null;
  },

  $VoiceRecEncode: function (samples, sourceRate) {
    var targetRate = 16000;
    var length = sourceRate === targetRate ? samples.length : Math.floor(samples.length * targetRate / sourceRate);
    var step = sourceRate / targetRate;
    var last = samples.length - 1;
    var buffer = new ArrayBuffer(44 + length * 2);
    var view = new DataView(buffer);
    function text(offset, value) {
      for (var i = 0; i < value.length; i++) view.setUint8(offset + i, value.charCodeAt(i));
    }
    text(0, 'RIFF');
    view.setUint32(4, 36 + length * 2, true);
    text(8, 'WAVE');
    text(12, 'fmt ');
    view.setUint32(16, 16, true);
    view.setUint16(20, 1, true);
    view.setUint16(22, 1, true);
    view.setUint32(24, targetRate, true);
    view.setUint32(28, targetRate * 2, true);
    view.setUint16(32, 2, true);
    view.setUint16(34, 16, true);
    text(36, 'data');
    view.setUint32(40, length * 2, true);
    for (var i = 0; i < length; i++) {
      var position = i * step;
      var left = Math.floor(position);
      var right = Math.min(left + 1, last);
      var value = samples[left] + (samples[right] - samples[left]) * (position - left);
      value = Math.max(-1, Math.min(1, value));
      view.setInt16(44 + i * 2, value < 0 ? value * 32768 : value * 32767, true);
    }
    return new Uint8Array(buffer);
  },

  $VoiceRecFinish__deps: ['$VoiceRec', '$VoiceRecRelease', '$VoiceRecEncode'],
  $VoiceRecFinish: function () {
    if (VoiceRec.state !== 2) return;
    var rate = VoiceRec.rate;
    VoiceRecRelease();
    if (VoiceRec.length === 0 || rate <= 0) {
      VoiceRec.chunks = [];
      VoiceRec.state = 4;
      return;
    }
    var samples = new Float32Array(VoiceRec.length);
    var offset = 0;
    for (var i = 0; i < VoiceRec.chunks.length; i++) {
      samples.set(VoiceRec.chunks[i], offset);
      offset += VoiceRec.chunks[i].length;
    }
    VoiceRec.chunks = [];
    VoiceRec.wav = VoiceRecEncode(samples, rate);
    VoiceRec.state = 3;
  },

  VoiceRecorder_IsSupported: function () {
    var hasMedia = typeof navigator !== 'undefined' && navigator.mediaDevices && navigator.mediaDevices.getUserMedia;
    var hasAudio = typeof window !== 'undefined' && (window.AudioContext || window.webkitAudioContext);
    return hasMedia && hasAudio ? 1 : 0;
  },

  VoiceRecorder_Start__deps: ['$VoiceRec', '$VoiceRecRelease', '$VoiceRecFinish'],
  VoiceRecorder_Start: function (maxSeconds) {
    if (VoiceRec.state === 1 || VoiceRec.state === 2) return;
    VoiceRecRelease();
    VoiceRec.chunks = [];
    VoiceRec.length = 0;
    VoiceRec.wav = null;
    VoiceRec.maxSeconds = maxSeconds > 0 ? maxSeconds : 15;
    VoiceRec.state = 1;

    var AudioContextType = window.AudioContext || window.webkitAudioContext;
    var context;
    try {
      context = new AudioContextType();
    } catch (e) {
      VoiceRec.state = 4;
      return;
    }
    VoiceRec.context = context;
    VoiceRec.rate = context.sampleRate;

    navigator.mediaDevices.getUserMedia({ audio: { channelCount: 1, echoCancellation: true, noiseSuppression: true } })
      .then(function (stream) {
        if (VoiceRec.state !== 1 || VoiceRec.context !== context) {
          // Cancelled while the browser was asking for permission.
          stream.getTracks().forEach(function (track) { track.stop(); });
          return;
        }
        VoiceRec.stream = stream;
        var limit = Math.floor(VoiceRec.maxSeconds * VoiceRec.rate);
        var source = context.createMediaStreamSource(stream);
        var processor = context.createScriptProcessor(4096, 1, 1);
        processor.onaudioprocess = function (event) {
          if (VoiceRec.state !== 2) return;
          var input = event.inputBuffer.getChannelData(0);
          var room = limit - VoiceRec.length;
          if (room > 0) {
            var copy = input.slice(0, Math.min(room, input.length));
            VoiceRec.chunks.push(copy);
            VoiceRec.length += copy.length;
          }
          if (VoiceRec.length >= limit) VoiceRecFinish();
        };
        source.connect(processor);
        // Chrome only runs the processor while it is connected to an output; it outputs silence.
        processor.connect(context.destination);
        VoiceRec.source = source;
        VoiceRec.processor = processor;
        VoiceRec.state = 2;
        if (context.state === 'suspended') context.resume();
      })
      .catch(function () {
        if (VoiceRec.context === context) {
          VoiceRecRelease();
          VoiceRec.state = 4;
        }
      });
  },

  VoiceRecorder_Stop__deps: ['$VoiceRec', '$VoiceRecRelease', '$VoiceRecFinish'],
  VoiceRecorder_Stop: function () {
    if (VoiceRec.state === 2) {
      VoiceRecFinish();
    } else if (VoiceRec.state === 1) {
      VoiceRecRelease();
      VoiceRec.state = 4;
    }
  },

  VoiceRecorder_Reset__deps: ['$VoiceRec', '$VoiceRecRelease'],
  VoiceRecorder_Reset: function () {
    VoiceRecRelease();
    VoiceRec.chunks = [];
    VoiceRec.length = 0;
    VoiceRec.wav = null;
    VoiceRec.state = 0;
  },

  VoiceRecorder_State__deps: ['$VoiceRec'],
  VoiceRecorder_State: function () {
    return VoiceRec.state;
  },

  VoiceRecorder_Seconds__deps: ['$VoiceRec'],
  VoiceRecorder_Seconds: function () {
    return VoiceRec.rate > 0 ? VoiceRec.length / VoiceRec.rate : 0;
  },

  VoiceRecorder_ResultLength__deps: ['$VoiceRec'],
  VoiceRecorder_ResultLength: function () {
    return VoiceRec.wav ? VoiceRec.wav.length : 0;
  },

  VoiceRecorder_CopyResult__deps: ['$VoiceRec'],
  VoiceRecorder_CopyResult: function (buffer, length) {
    if (!VoiceRec.wav) return;
    HEAPU8.set(VoiceRec.wav.subarray(0, Math.min(length, VoiceRec.wav.length)), buffer);
  }
};

mergeInto(LibraryManager.library, VoiceRecorderLib);
