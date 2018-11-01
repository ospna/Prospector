using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class Prospector : MonoBehaviour {

	static public Prospector 	S;

	[Header("Set in Inspector")]
	public TextAsset			deckXML;
    public TextAsset            layoutXML;
    public float xOffset = 3;
    public float yOffset = -2.5f;
    public Vector3 layoutCenter;

	[Header("Set Dynamically")]
	public Deck					deck;
    public Layout               layout;
    public List<CardProspector> drawPile;
    public Transform layoutAnchor;
    public CardProspector target;
    public List<CardProspector> tableau;
    public List<CardProspector> discardPile;

    void Awake(){
		S = this;
	}

	void Start() {
		deck = GetComponent<Deck> ();   // Get the deck
		deck.InitDeck (deckXML.text);   // Pass DeckXML to it
        Deck.Shuffle(ref deck.cards);   // This shuffles the deck

        layout = GetComponent<Layout>();
        layout.ReadLayout(layoutXML.text);

        drawPile = ConvertListCardsToListCardProspectors(deck.cards);
        LayoutGame();
	}

    List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD)
    {
        List<CardProspector> lCP = new List<CardProspector>();
        CardProspector tCP;
        foreach(Card tCD in lCD)
        {
            tCP = tCD as CardProspector;
            lCP.Add(tCP);
        }
        return (lCP);
    }

    // the draw function will pull a single card from the drawPile amd return it
    CardProspector Draw()
    {
        CardProspector cd = drawPile[0];
        drawPile.RemoveAt(0);
        return cd;
    }

    // positions the initial tableau of cards, aka the "mine"
    void LayoutGame()
    {
        // create an empty GO to serve as an anchor for the tab
        if(layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }

        CardProspector cp;
        foreach(SlotDef tSD in layout.slotDefs)     // iterate through all the SlotDefs in the layout.slotDefs as tSD
        {
            cp = Draw();        // pull a card from the top of the draw pile
            cp.faceUp = tSD.faceUp;     // set its faceUp to the value in SlotDef
            cp.transform.parent = layoutAnchor;     // replaces parent deck.deckAnchor which appears as _Deck to layoutAnchor

            // set the local position
            cp.transform.localPosition = new Vector3(
                layout.multiplier.x * tSD.x,
                layout.multiplier.y * tSD.y,
                -tSD.layerID);

            cp.layoutID = tSD.id;
            cp.slotDef = tSD;
            cp.state = eCardState.tableau;
            cp.SetSortingLayerName(tSD.layerName);  // set the sorting layers

            tableau.Add(cp);    // add this CP to the List<> tab

            MoveToTarget(Draw());       // set up the intial target card

            UpdateDrawPile();       // set up the draw pile
        }
    }

    // moves the current target to the discardPile
    void MoveToDiscard(CardProspector cd)
    {
        cd.state = eCardState.discard;  // set the state of the card to discard
        discardPile.Add(cd);        // add it to the dP List<>
        cd.transform.parent = layoutAnchor;

        // position this card on the dP
        cd.transform.localPosition = new Vector3(
            layout.multiplier.x * layout.discardPile.x,
            layout.multiplier.y * layout.discardPile.y,
            -layout.discardPile.layerID + 0.5f);

        cd.faceUp = true;
        // place it on top of the pile for depth sorting
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(-100 + discardPile.Count);
    }

    // make cd the new target card
    void MoveToTarget(CardProspector cd)
    {
        if (target != null) MoveToDiscard(target);      // if there is a target card, move it to dP
        target = cd;
        cd.state = eCardState.target;
        cd.transform.parent = layoutAnchor;

        // move to the target position
        // position this card on the dP
        cd.transform.localPosition = new Vector3(
            layout.multiplier.x * layout.discardPile.x,
            layout.multiplier.y * layout.discardPile.y,
            -layout.discardPile.layerID);

        cd.faceUp = true;
        // set the depth sorting
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(0);
    }

    void UpdateDrawPile()
    {
        CardProspector cd;
        // go thru all the cards in the dP
        for (int i = 0; i < drawPile.Count; i++)
        {
            cd = drawPile[i];
            cd.transform.parent = layoutAnchor;

            // position it correctly with the layout.drawPile.stagger
            Vector2 dpStagger = layout.drawPile.stagger;
            cd.transform.localPosition = new Vector3(
                layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x),
                layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y),
                -layout.drawPile.layerID + 0.1f * i);

            cd.faceUp = false;
            cd.state = eCardState.drawpile;
            // set depth sorting
            cd.SetSortingLayerName(layout.drawPile.layerName);
            cd.SetSortOrder(-10 * i);
        }
    }

    public void CardClicked(CardProspector cd)
    {
        // the reaction is determined by the state of the clicked card
        switch(cd.state)
        {
            case eCardState.target:
                break;

            case eCardState.drawpile:
                // clicking any card in the dP will draw the next card
                MoveToDiscard(target);      //Moves the target to the dP
                MoveToTarget(Draw());       // Moves the next drawn card to the target
                UpdateDrawPile();           // restacks the dP
                break;

            case eCardState.tableau:
                // clicking a card in the tab will check if it's a valid play
                break;
        }
    }

}
