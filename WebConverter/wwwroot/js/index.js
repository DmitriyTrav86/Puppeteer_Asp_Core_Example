"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/signalRHub").build();

connection.on("ReceiveMessage", function (param1, param2) {
    $("#progressBar").attr("aria-valuenow", param1).css('width', param1 + '%');
});

connection.start().then(function () {
    connection.invoke('getconnectionid').then((data) => {
        window.connectionId = data;
    });
}).catch(function (err) {
    return console.error(err.toString());
});