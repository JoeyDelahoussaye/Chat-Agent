import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class WebSocketService {
  private socket: WebSocket | null = null;
  private audioSubject = new Subject<ArrayBuffer>();

  constructor() { }

  connect(url: string): void {
    this.socket = new WebSocket(url);

    this.socket.onopen = () => {
      console.log('WebSocket connection established');
    };

    this.socket.onmessage = (event) => {
      if (event.data instanceof ArrayBuffer) {
        this.audioSubject.next(event.data);  // Emit audio buffer data
      }
    };

    this.socket.onerror = (error) => {
      console.error('WebSocket error:', error);
    };

    this.socket.onclose = () => {
      console.log('WebSocket connection closed');
    };
  }

  sendAudio(audioBuffer: ArrayBuffer): void {
    if (this.socket && this.socket.readyState === WebSocket.OPEN) {
      this.socket.send(audioBuffer);  // Send binary audio data over WebSocket
    }
  }

  getAudioMessages() {
    return this.audioSubject.asObservable();  // Observable for receiving audio from the backend
  }

  closeConnection(): void {
    if (this.socket) {
      this.socket.close();
    }
  }
}
