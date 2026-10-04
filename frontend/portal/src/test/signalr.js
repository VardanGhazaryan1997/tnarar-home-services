// Stand-in for @microsoft/signalr in tests: connections record their handlers, and tests push events with hub.emit().
export const LogLevel = { Warning: 3 }

export const hub = {
  connections: [],
  emit(event, payload) {
    this.connections.forEach((connection) => connection.handlers[event]?.forEach((handler) => handler(payload)))
  },
  reconnect() {
    this.connections.forEach((connection) => connection.reconnected?.())
  },
  failStart: false,
}

class FakeConnection {
  handlers = {}

  on(event, handler) {
    ;(this.handlers[event] ??= []).push(handler)
  }

  onreconnected(handler) {
    this.reconnected = handler
  }

  start() {
    if (hub.failStart) return Promise.reject(new Error('offline'))
    hub.connections.push(this)
    return Promise.resolve()
  }

  stop() {
    hub.connections = hub.connections.filter((connection) => connection !== this)
    return Promise.resolve()
  }
}

export class HubConnectionBuilder {
  withUrl(url, options) {
    this.url = url
    this.options = options
    return this
  }

  withAutomaticReconnect() {
    return this
  }

  configureLogging() {
    return this
  }

  build() {
    const connection = new FakeConnection()
    connection.url = this.url
    connection.options = this.options
    return connection
  }
}
