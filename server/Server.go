package main

import (
	"bufio"
	"encoding/binary"
	"fmt"
	"net"
	// "math/rand"
	"math/big"
)

// Message ...
type Message struct {
	Author Client
	Body   []byte
}

// Client ...
type Client struct {
	ID   uint32 // 4 byte
	Key  uint64
	Name string
	Conn net.Conn
}
func SendToClient (client Client, mess []byte, code bool){
	// if code{
	// 	for i := 1; i < len(mess)-1; i++ {
	// 		mess[i] = mess[i] ^ byte(client.Key);
	// 	}
	// }
	// fmt.Println("sended: ", mess)
	_, err := client.Conn.Write(mess)
	if err != nil {
		fmt.Println("send" + err.Error())
		return
	}
}

var clients map[uint32]Client
var messages chan Message

// RunServer ...
func RunServer() {
	messages = make(chan Message)
	clients = make(map[uint32]Client) 
	listener, _ := net.Listen("tcp", ":5555")
	fmt.Println("Run server")
	for {
		conn, err := listener.Accept()
		if err != nil {
			fmt.Println("Error, cant connect")
			continue
		}
		fmt.Println("Connected")
		fmt.Print("\n")
		go listenClient(conn)
	}
}

const endMess byte = 255
const commMess byte = 200
const commGetID byte = 201
const commDisconnect byte = 202
const commGetKey byte = 203
const commSendKey byte = 204
var privateKey  = big.NewInt(100)

func listenClient(c net.Conn){
	defer c.Close()
	dec := bufio.NewReader(c)
	var client Client
	var msg Message
	for{
		data, err := dec.ReadBytes(endMess)
		if err != nil{
			fmt.Println("error read, disconnect")
			delete(clients,client.ID)
			break
		}  
		// if client.ID != 0{
		// 	for i := 1; i < len(data)-1; i++ {
		// 		data[i] = data[i] ^ byte(client.Key);
		// 	}
		// }

		switch data[0] {
			case commMess:
				fmt.Println("mes: ", data)
				msg.Body = data
				SendMessages(msg)
			case commDisconnect:
				delete(clients,client.ID)
				return
			case commGetKey:
				g := big.NewInt(0).SetUint64(binary.LittleEndian.Uint64(data[1:9]))
				p := big.NewInt(0).SetUint64(binary.LittleEndian.Uint64(data[9:17]))
				B := big.NewInt(0).Exp(g,privateKey,p)
				fmt.Println("g: ", g)
				fmt.Println("p: ", p)
				fmt.Println("b: ", B)
				idData := make([]byte,10) 
				idData[0] = commGetKey
				idData[9] = endMess
				binary.LittleEndian.PutUint64(idData[1:9], B.Uint64())
				SendToClient(client,idData,false)
				A := big.NewInt(0).SetUint64(binary.LittleEndian.Uint64(data[17:25]))
				fmt.Println("a: ", A)
				
				B = big.NewInt(0).Exp(A,privateKey,p)
				client.Key = B.Uint64()
				fmt.Println("secret key: ", client.Key)
			// case commSendKey:
			// 	fmt.Println("keys")
				
			case commGetID:
				client = createClient(c,data)
				idData := make([]byte,6) 

				binary.LittleEndian.PutUint32(idData[1:5], client.ID)
				idData[0] = commGetID
				idData[5] = endMess
				fmt.Println("Send id")
				SendToClient(client,idData,false)
				msg.Author = client
				fmt.Println("Sended id")
				
			default:
				fmt.Println("err command")
			}
		}
	}

func SendMessages(mess Message) {
	for k:= range clients{
		if mess.Author.ID == k {
			continue
		}
		SendToClient(clients[k],mess.Body,true)
	}
}

var numID uint32 = 0

func createClient(connect net.Conn, data []byte) Client {
	var res Client
	numID ++ 
	res.Conn = connect
	res.ID = numID
	res.Name = string(data[1:])
	clients[numID] = res
	return res
}

func Pow(x uint64, y uint64)uint64{
	res := x
	for index := uint64(0); index < y-1; index++ {
		res *= x
	}
	return res
}

func main() {
	go RunServer()
	for{}
	// t := uint64(25000000)
	// r := Pow(31,10)
	// fmt.Println("ans: ", r)
}