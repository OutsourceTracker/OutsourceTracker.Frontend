/**
 * Passkey (WebAuthn) helper for Blazor WebAssembly.
 * Handles the browser WebAuthn ceremonies for registration and authentication.
 */

export async function createPasskey(optionsJson) {
    const options = JSON.parse(optionsJson);

    // Convert base64url to ArrayBuffer where needed
    options.challenge = base64UrlToArrayBuffer(options.challenge);
    options.user.id = base64UrlToArrayBuffer(options.user.id);

    if (options.excludeCredentials) {
        options.excludeCredentials = options.excludeCredentials.map(cred => ({
            ...cred,
            id: base64UrlToArrayBuffer(cred.id)
        }));
    }

    try {
        const credential = await navigator.credentials.create({
            publicKey: options
        });

        return {
            id: credential.id,
            rawId: arrayBufferToBase64Url(credential.rawId),
            type: credential.type,
            clientExtensionResults: credential.getClientExtensionResults 
                ? credential.getClientExtensionResults() 
                : {},
            response: {
                clientDataJSON: arrayBufferToBase64Url(credential.response.clientDataJSON),
                attestationObject: arrayBufferToBase64Url(credential.response.attestationObject),
                transports: credential.response.getTransports ? credential.response.getTransports() : []
            }
        };
    } catch (err) {
        console.error("Passkey creation failed:", err);
        throw err;
    }
}

export async function getPasskey(assertionOptionsJson) {
    const options = JSON.parse(assertionOptionsJson);

    options.challenge = base64UrlToArrayBuffer(options.challenge);

    if (options.allowCredentials) {
        options.allowCredentials = options.allowCredentials.map(cred => ({
            ...cred,
            id: base64UrlToArrayBuffer(cred.id)
        }));
    }

    try {
        const assertion = await navigator.credentials.get({
            publicKey: options
        });

        return {
            id: assertion.id,
            rawId: arrayBufferToBase64Url(assertion.rawId),
            type: assertion.type,
            clientExtensionResults: assertion.getClientExtensionResults 
                ? assertion.getClientExtensionResults() 
                : {},
            response: {
                clientDataJSON: arrayBufferToBase64Url(assertion.response.clientDataJSON),
                authenticatorData: arrayBufferToBase64Url(assertion.response.authenticatorData),
                signature: arrayBufferToBase64Url(assertion.response.signature),
                userHandle: assertion.response.userHandle ? arrayBufferToBase64Url(assertion.response.userHandle) : null
            }
        };
    } catch (err) {
        console.error("Passkey authentication failed:", err);
        throw err;
    }
}

// Helper functions for base64url <-> ArrayBuffer
function base64UrlToArrayBuffer(base64url) {
    const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    return bytes.buffer;
}

function arrayBufferToBase64Url(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    const base64 = btoa(binary);
    return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}