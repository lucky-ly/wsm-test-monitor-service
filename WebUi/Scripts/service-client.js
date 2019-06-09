/**
 * Create MonitorServiceClient instance
 *
 * @constructor 
 * @this  {MonitorServiceClient}
 * @param {string} serviceUrl - broadcaster url
 */
var MonitorServiceClient = function(serviceUrl) {
    const self = this;
    this.socket = null;
    this.serviceUrl = serviceUrl;
    
    /**
     * Open connection and set callbacks
     *
     * @param {function(data)} messageCallback
     * @param {function(event)} disconnectCallback
     */
    this.openConnection = function(messageCallback, disconnectCallback) {
        if (typeof (WebSocket) !== 'undefined') {
            self.socket = new WebSocket(self.serviceUrl);
        } else {
            self.socket = new MozWebSocket(self.serviceUrl);
        }

        self.socket.onmessage = function (msg) {
            const data = JSON.parse(msg.data);
            messageCallback(data);
        };

        self.socket.onclose = function (event) {
            disconnectCallback(event);
        };
    }
}