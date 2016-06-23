using System;

public class PROTOCOL
{
	public const int UDP_PRIVATE_CONNECT = 0;
	public const int UDP_PUBLIC_CONNECT = 1;
	public const int UDP_DATA = 2;

	public const int REQUEST_TYPE_SIGN_OUT = 1001;
	public const int REQUEST_TYPE_PING = 1002;
	public const int PONG = 1003;

	public const int REQUEST_TYPE_ENTER_CHANNEL = 1010;
	public const int REQUEST_TYPE_EXIT_CHANNEL = 1011;
	public const int REQUEST_TYPE_P2P_STATUS_SYNC = 1012;
	public const int REQUEST_TYPE_MIC_ON = 1014;
	public const int REQUEST_TYPE_MIC_OFF = 1015;
	public const int REQUEST_TYPE_CHECK_EXIST_CHANNEL = 1016;

	public const int RESPONSE_TYPE_NEW_USER_JOIN_CHANNEL = 1017;
	public const int RESPONSE_TYPE_OTHER_USER_JOIN_CHANNEL = 1018;
	public const int RESPONSE_TYPE_SIGN_IN = 1019;
}

