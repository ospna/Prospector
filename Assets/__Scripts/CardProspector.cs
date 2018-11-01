using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// an enum defines a variable type with a few prenamed values
public enum eCardState
{
    drawpile,
    tableau,
    target,
    discard
}

public class CardProspector : Card
{
    [Header("set Dynamically: CardProspector")]
    // using the enum
    public eCardState state = eCardState.drawpile;
    public List<CardProspector> hiddenBy = new List<CardProspector>(); // stores which other cards will kee this one face down
    public int layoutID;       // matches this card to the tableau XML if its a tableau card
    public SlotDef slotDef;    // stores info pulled in from the Layout <slot>

    // this allows the card to react when clicked
    public override void OnMouseUpAsButton()
    {
        Prospector.S.CardClicked(this);
        base.OnMouseUpAsButton();
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
