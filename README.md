# TME
This program allows to encode binary Turing machine into unsigned integer and decode it back into human readable form.  

To start conversion process, input and output should be defined.  
Generally, input and output parameters are specified as file names.  
However, two exceptions exists: when input file name have no extension, parameter is interpreted as base 10 representation of [description number](https://en.wikipedia.org/wiki/Description_number); when output parameter is empty, results are printed to console.  

Extension of file defines format of data, which it contains.  
* `.bin` files contain binary representation of number (base 256);
* `.bN` (where `N` is in range 2..36) files contain text representation of number (base N);
* `.jst` files contain text, compatible with [Turing machine simulator](https://morphett.info/turing/turing.html) (jsturing).

## Example
To convert from number and execute 4-state [busy beaver](https://en.wikipedia.org/wiki/Busy_beaver), you need to do following:  
1. Execute `TME 22580604002 bb4.jst` command;
2. Open [Turing machine simulator](https://morphett.info/turing/turing.html);
3. Delete `Initial input` value;
4. Replace contents of `Turing machine program` field with contents of `bb4.jst` file;
5. Press `Reset`;
6. Press `Run`.

## Encoding
Description numbers are created by splitting all possible unsigned integers into groups, corresponding to n-state binary Turing machines:  
* Starting at `0`, 1-state machines are located;
* Starting at `64`, 2-state machines are located;
* From `20800` to `16798015` 3-state machines are located;
* _And so on..._

Inside of each group, index of particular machine is calculated by representing its transition table as vector of digits of different bases and packing them into single number.  
For example, 2-state busy beaver can be encoded this way:  
* _Transition #1:_  
  * NewSymbol<sub>0</sub> = 1  
  * Direction<sub>0</sub> = 1 _(Right)_  
  * NewState<sub>0</sub> = 2  
  * NewSymbol<sub>1</sub> = 1  
  * Direction<sub>1</sub> = 0 _(Left)_  
  * NewState<sub>1</sub> = 2  
* _Transition #2:_  
  * NewSymbol<sub>0</sub> = 1  
  * Direction<sub>0</sub> = 0  
  * NewState<sub>0</sub> = 1  
  * NewSymbol<sub>1</sub> = 1  
  * Direction<sub>1</sub> = 1  
  * NewState<sub>1</sub> = 0 _(Halt)_  

     2 \* 2 \* 2 \* 2 \* 2 \* 2 +  
  **1** \* 3 \* 2 \* 2 \* 3 \* 2 \* 2 \* 3 \* 2 \* 2 \* 3 \* 2 +  
  **1** \* 3 \* 2 \* 2 \* 3 \* 2 \* 2 \* 3 \* 2 \* 2 \* 3 +  
  **2** \* 3 \* 2 \* 2 \* 3 \* 2 \* 2 \* 3 \* 2 \* 2 +  
  **1** \* 3 \* 2 \* 2 \* 3 \* 2 \* 2 \* 3 \* 2 +  
  **0** \* 3 \* 2 \* 2 \* 3 \* 2 \* 2 \* 3 +  
  **2** \* 3 \* 2 \* 2 \* 3 \* 2 \* 2 +  
  **1** \* 3 \* 2 \* 2 \* 3 \* 2 +  
  **0** \* 3 \* 2 \* 2 \* 3 +  
  **1** \* 3 \* 2 \* 2 +  
  **1** \* 3 \* 2 +  
  **1** \* 3 +  
  **0** =  
  20317
