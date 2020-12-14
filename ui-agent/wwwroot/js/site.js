
function onSignIn(googleUser) {
    var xhr = new XMLHttpRequest();
    var profile = googleUser.getBasicProfile();

    xhr.onreadystatechange = function () {
        if (xhr.readyState == XMLHttpRequest.DONE) {
            window.location.reload(false);
        }
    }

    xhr.open("POST", "/auth/googledone", true);
    xhr.setRequestHeader('Content-Type', 'application/json');

    xhr.send(JSON.stringify({
        ID: profile.getId(),
        FullName: profile.getName(),
        GivenName: profile.getGivenName(),
        FamilyName: profile.getFamilyName(),
        ImageURL: profile.getImageUrl(),
        Email: profile.getEmail(),
        IDToken: googleUser.getAuthResponse().id_token,
    }));
}

function googleLoggedInOK() {
    const Http = new XMLHttpRequest();
    const url = '/auth/';
    Http.open("GET", url);
    Http.send();

    Http.onreadystatechange = (e) => {
        console.log(Http.responseText)
    }
}
