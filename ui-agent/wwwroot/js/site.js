
function googleLoggedInOK() {
    const Http = new XMLHttpRequest();
    const url = '/auth/';
    Http.open("GET", url);
    Http.send();

    Http.onreadystatechange = (e) => {
        console.log(Http.responseText)
    }
}
