if (window.loadScript === undefined) {
	window.loadScript = function (url, callback, callbackError) {
		var script = document.createElement("script");
		script.type = "text/javascript";

		try {
			if (script.readyState) {  //IE
				script.onreadystatechange = function () {
					if (script.readyState === "loaded" || script.readyState === "complete") {
						script.onreadystatechange = null;
						callback();
					}
				};
			} else {
				//其餘瀏覽器支援onload
				script.onload = function () {
					callback();
				};
			}

			script.src = url;
			document.getElementsByTagName("head")[0].appendChild(script);
		} catch (e) {
			if (null !== callbackError) callbackError(e);
		}
	};
}

if (window.jQuery === undefined) {
	loadScript('../lib/jquery/dist/jquery.min.js',
		function () {
			initCAPIX();
		}, function () { });
} else {
	initCAPIX();
}

var capix;
function initCAPIX() {
	debugger;
	if ($.cookie === undefined) {
		loadScript('../js/jquery.cookie.js',
			function () {
				capix = createCAPIX();
			}, function () { });
	} else {
		capix = createCAPIX();
	}

	if ($.blockUI === undefined) {
		loadScript('../js/jquery.blockUI.js',
			function () {
			}, function () { });
	} 
}


function createCAPIX() {
    var errorCode;
	var resultObj = {};

	function pushField($form, name, value) {
		var $input = $('<input type="hidden" />');
		$input.attr('name', name);
		$input.val(value);
		$form.append($input);
	}

	var turn = 0;
	var opener;
	function checkSignature(txnID, remote, onSuccess, onFailed) {
		$.post(remote, { 'KeyID': txnID }, function (data) {
			if ($.isPlainObject(data)) {
				console.log(data);
				if (data.dataSignature !== undefined) {
					hideLoading();
					if (onSuccess) {
						onSuccess(data.dataToSign, data.dataSignature);
					}
				} else if (data.errorMessage !== undefined) {
					hideLoading();
					if (onFailed) {
						onFailed(data.errorMessage);
					}
				} else if (turn >= 360) {
					hideLoading();
					alert('作業逾時!!');
				} else if (opener.window == null) {
					hideLoading();
					alert('簽章已終止!!');
				} else {
					turn++;
					setTimeout(function () {
						checkSignature(txnID, remote, onSuccess, onFailed);
					}, 5000);
				}
			} else {
				$(data).appendTo($('body'));
			}
		});
	}

	function checkSigner(txnID, remote, onFailed) {

		var signer = window.open('https://localhost:57112/UXPKI/Ping', '_uxSign', 'popup');
		setTimeout(function () {
			if (signer.window == null) {
				return;
			} else {
				signer.close();
				if (onFailed) {
					onFailed();
				}
			}
		}, 1000);
	}

	function checkSignerRemote(txnID, remote, onFailed) {
		$.post(remote, { 'KeyID': txnID }, function (data) {
			if ($.isPlainObject(data)) {
				console.log(data);
				if (data.txnID !== undefined) {
				} else if (turn >= 10) {
					hideLoading();
					if (onFailed) {
						onFailed(data.errorMessage);
					}
				} else {
					turn++;
					setTimeout(function () {
						checkSignerRemote(txnID, remote, onFailed);
					}, 1000);
				}
			} else {
				$(data).appendTo($('body'));
			}
		});
	}

    var capixObj = {
		"CreateSigner": function () {
			var txnID = $.cookie('uxpki_id');
			if (txnID == undefined || txnID == '') {
				alert('尚未設定簽章交易序號!!');
				return;
			}
			var remote = $.cookie('uxpki_remote');
			if (remote == undefined || remote == '') {
				alert('尚未設定簽章交易主機!!');
				return;
			}
			var $esigner = $('<iframe style="display:none;"></iframe>');
			$esigner.attr('src', 'uxpki://localhost?RemoteHost=' + encodeURIComponent(remote) + '&TxnID=' + encodeURIComponent(txnID));
			$esigner.appendTo($('body'));
			//turn = 0;
			//setTimeout(function () {
			//	checkSignerRemote(txnID, remote, function () {
			//		alert('簽章元件未啟動或未安裝...');
			//	});
			//}, 5000);
			return $esigner;
		},
		"CheckSigner": function (onFailed) {

			var signer = window.open('https://localhost:57112/UXPKI/Ping', '_uxSign', 'popup');
			setTimeout(function () {
				if (signer.window == null) {
					return;
				} else {
					signer.close();
					if (onFailed) {
						onFailed();
					}
				}
			}, 1000);
		},
		"Sign": function (pbData, strSubject, onSuccess, onFailed) {
			var txnID = $.cookie('uxpki_id');
			if (txnID == undefined || txnID == '') {
				alert('尚未設定簽章交易序號!!');
				return;
			}
			var remote = $.cookie('uxpki_remote');
			if (remote == undefined || remote == '') {
				alert('尚未設定簽章交易主機!!');
				return;
			}
			opener = window.open('about:blank', '_uxSign', 'popup');
			var $form = $('<form action="https://localhost:57112/UXPKI/Sign" method="post" target="_uxSign"></form>');
			var $input = $('<input name="DataToSign" type="hidden" />');
			$input.val(pbData);
			$form.append($input);
			$input = $('<input name="Subject" type="hidden" />');
			$input.val(strSubject);
			$form.append($input);
			pushField($form, 'TxnID', txnID);
			pushField($form, 'RemoteHost', remote);
			$form.appendTo('body').submit().remove();

			showLoading();
			turn = 0;
			setTimeout(function () {
				checkSignature(txnID, remote, onSuccess, onFailed);
			}, 5000);

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

function showLoading(autoHide, onBlock) {
	$.blockUI({
		message: '<img src="../images/loading.gif" /><h1>Loading</h1>',
		css: {
			border: 'none',
			padding: '15px',
			backgroundColor: '#000',
			'-webkit-border-radius': '10px',
			'-moz-border-radius': '10px',
			opacity: .5,
			color: '#fff'
		},
		// 背景圖層
		overlayCSS: {
			backgroundColor: '#3276B1',
			opacity: 0.6,
			cursor: 'wait'
		},
		onBlock: onBlock
	});

	if (autoHide)
		setTimeout($.unblockUI, 5000);
}

function hideLoading() {
	$.unblockUI();
}

$.fn.serializeObject = function () {
	var o = {};
	var a = this.serializeArray();
	$.each(a, function () {
		if (o[this.name] !== undefined) {
			if (!o[this.name].push) {
				o[this.name] = [o[this.name]];
			}
			o[this.name].push(this.value || '');
		} else {
			o[this.name] = this.value || '';
		}
	});
	return o;
};

$.fn.launchDownload = function (url, params, target, loading) {

	var data = this.serializeObject();
	if (params) {
		$.extend(data, params);
	}

	if (loading) {
		token = (new Date()).getTime();
		data.FileDownloadToken = token;
	}

	var form = $('<form></form>').attr('action', url).attr('method', 'post');//.attr('target', '_blank');
	if (target) {
		form.attr('target', target);
		if (window.frames[target] == null) {
			$('<iframe>')
				.css('display', 'none')
				.attr('name', target).appendTo($('body'));
		}
	}

	Object.keys(data).forEach(function (key) {
		var value = data[key];

		if (value instanceof Array) {
			value.forEach(function (v) {
				form.append($("<input></input>").attr('type', 'hidden').attr('name', key).attr('value', v));
			});
		} else {
			form.append($("<input></input>").attr('type', 'hidden').attr('name', key).attr('value', value));
		}

	});

	if (loading) {
		showLoading();
		fileDownloadCheckTimer = window.setInterval(function () {
			var cookieValue = $.cookie('FileDownloadToken');
			if (cookieValue == token)
				finishDownload();
		}, 1000);
	}

	//send request
	form.appendTo('body').submit().remove();
};

function finishDownload() {
	window.clearInterval(fileDownloadCheckTimer);
	$.removeCookie('fileDownloadToken'); //clears this cookie value
	hideLoading();
}

