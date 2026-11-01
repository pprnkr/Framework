package com.nyonyo.framework.firebase;

import android.app.Activity;
import android.os.CancellationSignal;
import android.os.Handler;
import android.os.Looper;

import androidx.annotation.Keep;
import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialCancellationException;
import androidx.credentials.exceptions.GetCredentialException;

import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;

/**
 * Unity(GoogleSignInAndroid.cs)에서 부르는 구글 로그인입니다. Android Credential Manager로 구글 계정을 고르게 하고 ID 토큰을 돌려줍니다.
 * C#이 이름으로 찾아 부르므로 코드 축소(R8)에서 빠지지 않게 @Keep을 붙입니다.
 */
@Keep
public final class GoogleSignInBridge {
    // GoogleSignInAndroid.cs의 결과 코드와 같아야 합니다.
    public static final int RESULT_SUCCESS = 0;
    public static final int RESULT_CANCELLED = 1;
    public static final int RESULT_FAILED = 2;
    public static final int RESULT_NOT_CONFIGURED = 3;

    /** 결과를 받는 C# 쪽 인터페이스입니다. */
    @Keep
    public interface Callback {
        void onCompleted(int result, String idToken, String message);
    }

    private GoogleSignInBridge() {
    }

    /**
     * 구글 계정 선택 창을 띄웁니다. 결과는 UI 스레드에서 callback으로 옵니다.
     *
     * @param webClientId 비어 있으면 google-services.json에서 만든 리소스 default_web_client_id를 씁니다.
     */
    @Keep
    public static void signIn(final Activity activity, final String webClientId, final Callback callback) {
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    start(activity, webClientId, callback);
                } catch (Exception e) {
                    callback.onCompleted(RESULT_FAILED, null, e.toString());
                }
            }
        });
    }

    private static void start(Activity activity, String webClientId, final Callback callback) {
        String clientId = webClientId;
        if (clientId == null || clientId.isEmpty()) {
            clientId = readDefaultWebClientId(activity);
        }
        if (clientId == null || clientId.isEmpty()) {
            callback.onCompleted(RESULT_NOT_CONFIGURED, null, "default_web_client_id not found");
            return;
        }

        GetSignInWithGoogleOption option = new GetSignInWithGoogleOption.Builder(clientId).build();
        GetCredentialRequest request = new GetCredentialRequest.Builder().addCredentialOption(option).build();
        CredentialManager manager = CredentialManager.Companion.create(activity);
        final Handler handler = new Handler(Looper.getMainLooper());

        manager.getCredentialAsync(activity, request, new CancellationSignal(), handler::post,
                new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                    @Override
                    public void onResult(GetCredentialResponse response) {
                        handleResponse(response, callback);
                    }

                    @Override
                    public void onError(GetCredentialException e) {
                        if (e instanceof GetCredentialCancellationException) {
                            callback.onCompleted(RESULT_CANCELLED, null, e.getMessage());
                        } else {
                            callback.onCompleted(RESULT_FAILED, null, e.getType() + ": " + e.getMessage());
                        }
                    }
                });
    }

    private static void handleResponse(GetCredentialResponse response, Callback callback) {
        Credential credential = response.getCredential();
        if (!(credential instanceof CustomCredential)
                || !GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL.equals(credential.getType())) {
            callback.onCompleted(RESULT_FAILED, null, "Unexpected credential type: " + credential.getType());
            return;
        }

        try {
            GoogleIdTokenCredential google = GoogleIdTokenCredential.Companion.createFrom(credential.getData());
            callback.onCompleted(RESULT_SUCCESS, google.getIdToken(), null);
        } catch (Exception e) {
            callback.onCompleted(RESULT_FAILED, null, e.toString());
        }
    }

    private static String readDefaultWebClientId(Activity activity) {
        int id = activity.getResources().getIdentifier("default_web_client_id", "string", activity.getPackageName());
        return id == 0 ? null : activity.getString(id);
    }
}
