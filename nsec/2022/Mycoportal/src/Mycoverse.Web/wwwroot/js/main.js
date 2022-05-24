let u = "guest";

function getSessionInfo() {
    var oReq = new XMLHttpRequest();
    oReq.onload = (e) => {
        r = JSON.parse(oReq.responseText);
        u = r["Username"];
        console.log("whoami: " + u);
    };
    oReq.open("GET", "/whoami");
    oReq.send();
}

function getApiKeys() {
    let api = new WebSocket(`ws://${window.location.host}/ws`);
    api.onmessage = (e) => 
    {
        console.log(e);
        api.close();
    }
    api.onopen = (e) => {
        api.send('{"Type": 1, "Username": "guest", "ApiKey": "null"}\n');
    }
}

getSessionInfo();