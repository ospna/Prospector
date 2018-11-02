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
    public Vector2 fsPosMid = new Vector2( 0.5f, 0.90f);
    public Vector2 fsPosRun = new Vector2( 0.5f, 0.75f);
    public Vector2 fsPosMid2 = new Vector2( 0.4f, 1.0f);
    public Vector2 fsPosEnd = new Vector2( 0.5f, 0.95f);


    [Header("Set Dynamically")]
	public Deck					deck;
    public Layout               layout;
    public List<CardProspector> drawPile;
    public Transform layoutAnchor;
    public CardProspector target;
    public List<CardProspector> tableau;
    public List<CardProspector> discardPile;
    public FloatingScore fsRun;

    void Awake()
    {
		S = this;
	}

	void Start()
    {
        Scoreboard.S.score = ScoreManager.SCORE;

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
        return(cd);
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
        }

        // set which cards are hiding others
        foreach (CardProspector tCP in tableau)
        {
            foreach(int hid in tCP.slotDef.hiddenBy)
            {
                cp = FindCardByLayoutID(hid);
                tCP.hiddenBy.Add(cp);
            }
        }

        MoveToTarget(Draw());       // set up the intial target card

        UpdateDrawPile();       // set up the draw pile
    }

    // convert from the layutID int to the CP with that ID
    CardProspector FindCardByLayoutID(int layoutID)
    {
        // search thru all cards in the tab list<>
        foreach(CardProspector tCP in tableau)
        {
            if(tCP.layoutID == layoutID)
            {
                return (tCP);
            }
        }
        return (null);
    }

    // this turns cards in the Mine faceup or down
    void SetTableauFaces()
    {
        foreach(CardProspector cd in tableau)
        {
            bool faceUp = true;
            foreach(CardProspector cover in cd.hiddenBy)
            {
                // if either of the covering cards are in the tab
                if(cover.state == eCardState.tableau)
                {
                    faceUp = false;
                }
            }
            cd.faceUp = faceUp;     // set the value on the card
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
                ScoreManager.EVENT(eScoreEvent.draw);
                FloatingScoreHandler(eScoreEvent.draw);
                break;

            case eCardState.tableau:
                // clicking a card in the tab will check if it's a valid play
                bool validMatch = true;
                if(!cd.faceUp)
                {
                    validMatch = false;
                }
                if(!AdjacentRank(cd, target))
                {
                    validMatch = false;
                }
                if(!validMatch)
                {
                    return;
                }
                tableau.Remove(cd);
                MoveToTarget(cd);
                SetTableauFaces();
                ScoreManager.EVENT(eScoreEvent.mine);
                FloatingScoreHandler(eScoreEvent.mine);
                break;
        }
        CheckForGameOver();
    }

    void CheckForGameOver()
    {
        // if the tableau is empty, the game is over
        if(tableau.Count == 0)
        {
            GameOver(true);
            return;
        }

        // if there are still cards in the draw pile, the game is not over
        if(drawPile.Count > 0)
        {
            return;
        }

        // check for remaining valid plays
        foreach (CardProspector cd in tableau)
        {
            if(AdjacentRank(cd, target))
            {
                return;
            }
        }

        // since there are no valid plays, the game is over and you lose
        GameOver(false);
    }

    // called when the game is over
    void GameOver(bool won)
    {
        if(won)
        {
            //print("Game Over. You won! (:");
            ScoreManager.EVENT(eScoreEvent.gameWin);
            FloatingScoreHandler(eScoreEvent.gameWin);
        }
        else
        {
            //print("Game Over. You lost! :(");
            ScoreManager.EVENT(eScoreEvent.gameLoss);
            FloatingScoreHandler(eScoreEvent.gameLoss);
        }
        SceneManager.LoadScene("__Prospector_Scene_0");
    }

    // return ture if the two cards are adjacent in rank (A & K wrap around)
    public bool AdjacentRank(CardProspector c0, CardProspector c1)
    {
        // if either card is face-dwon, its not adjacent
        if(!c0.faceUp || !c1.faceUp)
        {
            return (false);
        }

        // if they are 1 apart, they are adjacent
        if(Mathf.Abs(c0.rank - c1.rank) == 1)
        {
            return (true);
        }

        // if one is Ace and the other is King, they are adjacent
        if(c0.rank == 1 && c1.rank == 13)
        {
            return (true);
        }
        if (c0.rank == 13 && c1.rank == 1)
        {
            return (true);
        }

        return (false);
    }

    void FloatingScoreHandler(eScoreEvent evt)
    {
        List<Vector2> fsPts;

        switch(evt)         // same things need to happen regardless of outcome
        {
            case eScoreEvent.draw:
            case eScoreEvent.gameWin:
            case eScoreEvent.gameLoss:
                if(fsRun != null)           // add the fsRun to the scoreboard score
                {
                    // create points for the bezier curve
                    fsPts = new List<Vector2>();
                    fsPts.Add(fsPosRun);
                    fsPts.Add(fsPosMid2);
                    fsPts.Add(fsPosEnd);
                    fsRun.reportFinishTo = Scoreboard.S.gameObject;
                    fsRun.Init(fsPts, 0, 1);
                    fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 });
                    fsRun = null;           // clear so its created agin
                }
                break;

            case eScoreEvent.mine:          // remove a mine card
                FloatingScore fs;
                Vector2 p0 = Input.mousePosition;            // move it from the mousePos to fsPosRun
                p0.x /= Screen.width;
                p0.y /= Screen.height;
                fsPts = new List<Vector2>();
                fsPts.Add(p0);
                fsPts.Add(fsPosMid);
                fsPts.Add(fsPosRun);
                fs = Scoreboard.S.CreateFloatingScore(ScoreManager.CHAIN, fsPts);
                fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });

                if(fsRun == null)
                {
                    fsRun = fs;
                    fsRun.reportFinishTo = null;
                }
                else
                {
                    fs.reportFinishTo = fsRun.gameObject;
                }
                break;
        }

    }

}
