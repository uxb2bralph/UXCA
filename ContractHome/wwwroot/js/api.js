function postData(url, data) {
  let headers = {
    "Content-Type": "application/json",
    "Accept": "application/json",
  }
  return fetch(url, {
    method: "POST",
    headers,
    body: JSON.stringify(data),
  }).then((response) => response.json());
}

function getData(url, data) {
  let headers = {
    "Content-Type": "application/json",
    "Accept": "application/json",
  }
  return fetch(url, {
    method: "GET",
    headers,
    body: JSON.stringify(data),
  }).then((response) => response.json());
}
