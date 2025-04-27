using UnityEngine;


/**
 * Contains credentials to a RabbitMQ instance. See https://www.rabbitmq.com/.
 * Also contains link to running DatAR companion app (see datar-web repository).
 */
[CreateAssetMenu(fileName = "MessageBrokerConfig", menuName = "ScriptableObjects/MessageBrokerConfig")]
public class MessageBrokerConfig : ScriptableObject
{
    public string brokerUrl;
    public string brokerUsername;
    public string brokerPassword;
    public string webApplicationUrl;
}