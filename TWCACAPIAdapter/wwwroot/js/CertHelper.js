var capix = createCAPIX();

function createCAPIX() {
    var errorCode;
	var resultObj = {};

    var capixObj = {
        "Sign": function (pbData, strSubject, iFlags, keyUsage) {
			var url = 'https://localhost:57112/TWCACAPI/Sign';
			var xhr = new XMLHttpRequest();
			xhr.open('POST', url, false);
			xhr.setRequestHeader("Content-Type", "application/json");
			var onLoadHandler = function (event) {
				try {
					resultObj = JSON.parse(this.responseText);
				}
				catch (err) {
					resultObj = {};
					if (this.responseText != undefined) {
						resultObj.result = this.responseText;
					}
				}
			}
			xhr.onload = onLoadHandler;
			DataObj =
			{
				"DataToSign": pbData,
				"Subject": strSubject,
				"Flags": iFlags,
				"KeyUsage": keyUsage,
			};
				
			var readyDataObj = JSON.stringify(DataObj);

			try {
				xhr.send(readyDataObj);
			}
			catch (err) {
				console.log(err);
				return undefined;
			}
			return resultObj.dataSignature;
		},
		"SignXml": function (pbData, strSubject, iFlags, keyUsage) {
			var url = 'https://localhost:57112/TWCACAPI/SignXml';
			var xhr = new XMLHttpRequest();
			xhr.open('POST', url, false);
			xhr.setRequestHeader("Content-Type", "application/json");
			var onLoadHandler = function (event) {
				try {
					resultObj = JSON.parse(this.responseText);
				}
				catch (err) {
					resultObj = {};
					if (this.responseText != undefined) {
						resultObj.result = this.responseText;
					}
				}
			}
			xhr.onload = onLoadHandler;
			DataObj =
			{
				"DataToSign": pbData,
				"Subject": strSubject,
				"Flags": iFlags,
				"KeyUsage": keyUsage,
			};

			var readyDataObj = JSON.stringify(DataObj);

			try {
				xhr.send(readyDataObj);
			}
			catch (err) {
				console.log(err);
				return undefined;
			}
			return resultObj.dataSignature;
		},
		"SignCms": function (pbData, strSubject, iFlags, keyUsage) {
			var url = 'https://localhost:57112/TWCACAPI/SignCms';
			var xhr = new XMLHttpRequest();
			xhr.open('POST', url, false);
			xhr.setRequestHeader("Content-Type", "application/json");
			var onLoadHandler = function (event) {
				try {
					resultObj = JSON.parse(this.responseText);
				}
				catch (err) {
					resultObj = {};
					if (this.responseText != undefined) {
						resultObj.result = this.responseText;
					}
				}
			}
			xhr.onload = onLoadHandler;
			DataObj =
			{
				"DataToSign": pbData,
				"Subject": strSubject,
				"Flags": iFlags,
				"KeyUsage": keyUsage,
			};

			var readyDataObj = JSON.stringify(DataObj);

			try {
				xhr.send(readyDataObj);
			}
			catch (err) {
				console.log(err);
				return undefined;
			}
			return resultObj.dataSignature;
		},
		"CreatePKCS10": function (firstName, keyStore) {
			var url = 'https://localhost:57112/TWCACAPI/CreatePKCS10';
			var xhr = new XMLHttpRequest();
			xhr.open('POST', url, false);
			xhr.setRequestHeader("Content-Type", "application/json");
			var onLoadHandler = function (event) {
				try {
					resultObj = JSON.parse(this.responseText);
				}
				catch (err) {
					resultObj = {};
					if (this.responseText != undefined) {
						resultObj.result = this.responseText;
					}
				}
			}
			xhr.onload = onLoadHandler;
			DataObj =
			{
				"FirstName": firstName,
				"KeyStore": keyStore,
			};

			var readyDataObj = JSON.stringify(DataObj);

			try {
				xhr.send(readyDataObj);
			}
			catch (err) {
				console.log(err);
				return undefined;
			}
			return resultObj.pkcs10;
		},
		"BuildCertificate": function (csr) {
			var url = 'https://localhost:57112/TWCACAPI/BuildCertificate';
			var xhr = new XMLHttpRequest();
			xhr.open('POST', url, false);
			xhr.setRequestHeader("Content-Type", "application/json");
			var onLoadHandler = function (event) {
				try {
					resultObj = JSON.parse(this.responseText);
				}
				catch (err) {
					resultObj = {};
					if (this.responseText != undefined) {
						resultObj.result = this.responseText;
					}
				}
			}
			xhr.onload = onLoadHandler;
			DataObj =
			{
				"CSR": csr,
			};

			var readyDataObj = JSON.stringify(DataObj);

			try {
				xhr.send(readyDataObj);
			}
			catch (err) {
				console.log(err);
				return undefined;
			}
			return resultObj;
		},
		"InstallCertificate": function (pkcs7Cert) {
			var url = 'https://localhost:57112/TWCACAPI/InstallCertificate';
			var xhr = new XMLHttpRequest();
			xhr.open('POST', url, false);
			xhr.setRequestHeader("Content-Type", "application/json");
			var onLoadHandler = function (event) {
				try {
					resultObj = JSON.parse(this.responseText);
				}
				catch (err) {
					resultObj = {};
					if (this.responseText != undefined) {
						resultObj.result = this.responseText;
					}
				}
			}
			xhr.onload = onLoadHandler;
			DataObj =
			{
				"Pkcs7": pkcs7Cert,
			};

			var readyDataObj = JSON.stringify(DataObj);

			try {
				xhr.send(readyDataObj);
			}
			catch (err) {
				console.log(err);
				return undefined;
			}
			return resultObj;
		},
		"GetErrorCode": function () {
			return resultObj.errorCode;
		},
		"ResultObject": resultObj,
    };

    return capixObj;
}
