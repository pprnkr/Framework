// Unity(AppleSignInIOS.cs)에서 부르는 애플 로그인(Sign in with Apple)입니다.
// AuthenticationServices로 로그인 창을 띄우고, 애플 ID 토큰을 C# 콜백으로 돌려줍니다.
#import <Foundation/Foundation.h>
#import <AuthenticationServices/AuthenticationServices.h>
#import "UnityAppController.h"

// AppleSignInIOS.cs의 결과 코드와 같아야 합니다.
static const int NyonyoAppleResultSuccess = 0;
static const int NyonyoAppleResultCancelled = 1;
static const int NyonyoAppleResultFailed = 2;
static const int NyonyoAppleResultNotSupported = 3;

typedef void (*NyonyoAppleSignInCallback)(int result, const char* idToken, const char* message);

API_AVAILABLE(ios(13.0))
@interface NyonyoAppleSignInDelegate : NSObject <ASAuthorizationControllerDelegate, ASAuthorizationControllerPresentationContextProviding>
@property (nonatomic, assign) NyonyoAppleSignInCallback callback;
@property (nonatomic, strong) ASAuthorizationController* controller;
@end

// ASAuthorizationController는 delegate를 약하게 붙잡으므로, 로그인이 끝날 때까지 여기서 붙잡아 둡니다.
static NSMutableSet* s_activeDelegates = nil;

@implementation NyonyoAppleSignInDelegate

- (void)finishWithResult:(int)result idToken:(NSString*)idToken message:(NSString*)message
{
    NyonyoAppleSignInCallback callback = self.callback;
    self.callback = NULL;
    self.controller = nil;
    [s_activeDelegates removeObject:self];

    if (callback != NULL)
        callback(result, idToken != nil ? [idToken UTF8String] : NULL, message != nil ? [message UTF8String] : NULL);
}

- (void)authorizationController:(ASAuthorizationController*)controller didCompleteWithAuthorization:(ASAuthorization*)authorization
{
    if (![authorization.credential isKindOfClass:[ASAuthorizationAppleIDCredential class]])
    {
        [self finishWithResult:NyonyoAppleResultFailed idToken:nil message:@"Unexpected credential type."];
        return;
    }

    ASAuthorizationAppleIDCredential* credential = (ASAuthorizationAppleIDCredential*)authorization.credential;
    if (credential.identityToken == nil)
    {
        [self finishWithResult:NyonyoAppleResultFailed idToken:nil message:@"Apple did not return an identity token."];
        return;
    }

    NSString* idToken = [[NSString alloc] initWithData:credential.identityToken encoding:NSUTF8StringEncoding];
    [self finishWithResult:NyonyoAppleResultSuccess idToken:idToken message:nil];
}

- (void)authorizationController:(ASAuthorizationController*)controller didCompleteWithError:(NSError*)error
{
    int result = error.code == ASAuthorizationErrorCanceled ? NyonyoAppleResultCancelled : NyonyoAppleResultFailed;
    [self finishWithResult:result idToken:nil message:error.localizedDescription];
}

- (ASPresentationAnchor)presentationAnchorForAuthorizationController:(ASAuthorizationController*)controller
{
    return UnityGetMainWindow();
}

@end

extern "C" void NyonyoAppleSignIn_Start(const char* hashedNonce, NyonyoAppleSignInCallback callback)
{
    if (@available(iOS 13.0, *))
    {
        ASAuthorizationAppleIDProvider* provider = [[ASAuthorizationAppleIDProvider alloc] init];
        ASAuthorizationAppleIDRequest* request = [provider createRequest];
        request.requestedScopes = @[ASAuthorizationScopeFullName, ASAuthorizationScopeEmail];
        request.nonce = [NSString stringWithUTF8String:hashedNonce];

        NyonyoAppleSignInDelegate* signInDelegate = [[NyonyoAppleSignInDelegate alloc] init];
        signInDelegate.callback = callback;
        signInDelegate.controller = [[ASAuthorizationController alloc] initWithAuthorizationRequests:@[request]];
        signInDelegate.controller.delegate = signInDelegate;
        signInDelegate.controller.presentationContextProvider = signInDelegate;

        if (s_activeDelegates == nil)
            s_activeDelegates = [[NSMutableSet alloc] init];
        [s_activeDelegates addObject:signInDelegate];

        [signInDelegate.controller performRequests];
    }
    else
    {
        callback(NyonyoAppleResultNotSupported, NULL, "Sign in with Apple requires iOS 13 or later.");
    }
}
