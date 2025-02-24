import { Component, OnInit, OnDestroy } from '@angular/core';
import { WebSocketService } from './websocket.service';

@Component({
  selector: 'app-audio',
  templateUrl: './audio.component.html',
  styleUrls: []
})
export class AudioComponent implements OnInit, OnDestroy {
  private audioContext: AudioContext | null = null;
  private analyserNode: AnalyserNode | null = null;
  private microphoneStream: MediaStream | null = null;

  constructor(private wsService: WebSocketService) { }

  ngOnInit(): void {
    this.wsService.connect('wss://localhost:5001/ws');  // Connect to your WebSocket server
    this.wsService.getAudioMessages().subscribe((audioBuffer: ArrayBuffer) => {
      this.playAudio(audioBuffer);
    });
  }

  ngOnDestroy(): void {
    this.wsService.closeConnection();
    if (this.microphoneStream) {
      this.microphoneStream.getTracks().forEach(track => track.stop());  // Stop the microphone stream
    }
  }

  // Start capturing microphone audio and sending it over WebSocket
  startStreaming(): void {
    if (navigator.mediaDevices) {
      navigator.mediaDevices.getUserMedia({ audio: true })
        .then((stream) => {
          this.microphoneStream = stream;

          // Create an AudioContext and AnalyserNode for real-time audio processing
          this.audioContext = new AudioContext();
          const source = this.audioContext.createMediaStreamSource(stream);
          this.analyserNode = this.audioContext.createAnalyser();
          source.connect(this.analyserNode);

          // Now capture audio data continuously and send it to the backend
          this.streamAudioToServer();
        })
        .catch((error) => {
          console.error('Error accessing the microphone:', error);
        });
    }
  }

  // Stop streaming
  stopStreaming(): void {
    if (this.audioContext) {
      this.audioContext.close();
    }
    if (this.microphoneStream) {
      this.microphoneStream.getTracks().forEach(track => track.stop());
    }
  }

  // Continuously stream audio data to the WebSocket server
  private streamAudioToServer(): void {
    if (this.analyserNode) {
      const bufferLength = this.analyserNode.frequencyBinCount;
      const buffer = new Float32Array(bufferLength);

      // Set up an interval to send audio data in real-time
      const processAudio = () => {
        this.analyserNode?.getFloatFrequencyData(buffer);
        const audioData = this.convertToBinaryData(buffer);
        this.wsService.sendAudio(audioData);  // Send the binary audio data to the backend
      };

      setInterval(processAudio, 100);  // Adjust interval as needed
    }
  }

  // Convert audio data (e.g., frequency buffer) into binary data (you can adjust as needed)
  private convertToBinaryData(buffer: Float32Array): ArrayBuffer {
    const byteArray = new Uint8Array(buffer.length * 4);  // 4 bytes per float (32-bit)
    buffer.forEach((value, index) => {
      new DataView(byteArray.buffer).setFloat32(index * 4, value, true);
    });
    return byteArray.buffer;
  }

  // Play the received audio (e.g., MP3) from OpenAI API
  private playAudio(audioBuffer: ArrayBuffer): void {
    const audioContext = new AudioContext();
    audioContext.decodeAudioData(audioBuffer)
      .then((audioData) => {
        const source = audioContext.createBufferSource();
        source.buffer = audioData;
        source.connect(audioContext.destination);
        source.start();
      })
      .catch((error) => {
        console.error('Error decoding audio:', error);
      });
  }
}
