var start = function () {
	var inc = document.getElementById('incoming');
	var inc1 = document.getElementById('WorkoutStatus');
	var inc2 = document.getElementById('UserStatus');
	var inc3 = document.getElementById('WorkoutDef');
	var wsImpl = window.WebSocket || window.MozWebSocket;
	var form = document.getElementById('sendForm');
	var input = document.getElementById('sendText');

	inc.innerHTML += "connecting to server ..<br/>";

	// create a new websocket and connect
	var ip = 'ws://' + location.hostname + ':8181/';

	window.ws = new wsImpl(ip);

	// when data is comming from the server, this metod is called
	ws.onmessage = function (evt) {
		//      inc.innerHTML += evt.data + '<br/>';
		inc.innerHTML = evt.data + '<br/>';

		var jsonData = JSON.parse(evt.data);

		if (jsonData.title) {
			inc3.innerHTML = "Selected Workout is </br>" + JSON.stringify(jsonData, null, "\t");
		};

		if (jsonData.wEA) {
			inc1.innerHTML = JSON.stringify(jsonData.wEA, null, "\t");

			inc2.innerHTML = "users:</br>"
			for (var i in jsonData.uEAs) {
				inc2.innerHTML += JSON.stringify(jsonData.uEAs[i], null, "\t");

			}
		};

	};

	// when the connection is established, this method is called
	ws.onopen = function () {
		inc.innerHTML += '.. connection open<br/>';
	};

	// when the connection is closed, this method is called
	ws.onclose = function () {
		inc.innerHTML += '.. connection closed<br/>';
	}

	form.addEventListener('submit', function (e) {
		e.preventDefault();
		var val = input.value;
		ws.send(val);
		input.value = "";
	});

}
window.onload = start;
